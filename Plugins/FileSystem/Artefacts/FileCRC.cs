using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using Mono.Math;

namespace Artefacts.FileSystem
{
	public class FileCRC
	{
		public static ServiceStack.Logging.ILog Log = Artefact.LogFactory.GetLogger(typeof(FileCRC));


		private static Semaphore _crcSemaphore;

		private static BigInteger _mallocTotal = new BigInteger(0);

		private const int _readBufferSize = 8 * 1024 * 1024;

		public static int QueueCount {
			get { return Volatile.Read(ref _queueCount); }
			set { Volatile.Write(ref _queueCount, value); }
		}
		private static int _queueCount = 0;
		private static readonly object QueueMonitorLock = new object();

		
		public static bool WaitForEmptyQueue()
		{
			return WaitForEmptyQueue(Timeout.Infinite);
		}
		public static bool WaitForEmptyQueue(int timeout)
		{
//			Monitor.Enter(QueueMonitorLock);
//			while (QueueCount > 0)
//			{
//				Monitor.Exit(QueueMonitorLock);
//				Monitor.Enter(QueueMonitorLock);
//			}
//			Monitor.Exit(QueueMonitorLock);
		return WaitForQueueCount(0, timeout);
		}

		public static bool WaitForQueueCount(int count)
		{
		return WaitForQueueCount(count, Timeout.Infinite);
		}
		public static bool WaitForQueueCount(int count, int timeout)
		{
//			while (true)
			while (QueueCount > count)
			{
				Log.DebugFormat("FileCRC.WaitForQueueCount({0}, {1}): FileCRC.QueueCount={2}>{3}, waiting...\n", count, timeout, QueueCount, count);
				Thread.Sleep(3333);
//					Monitor.Enter(QueueMonitorLock);
//			Monitor.Exit(QueueMonitorLock);
			}
			return true;
		}

		public class Region
		{
			/// <summary>Gets the size of the component</summary>
			public const int ComponentSize = sizeof(Int64);

			/// <summary>Gets the start of the CRC Region</summary>
			public long Start { get; private set; }

			/// <summary>Gets the finish of the CRC Region</summary>
			public long Finish { get; private set; }

			/// <summary>Gets the size of the CRC Region (byte count from start to finish, inclusive of both)</summary>
			public int Size { get; private set; }

			/// <summary>Gets the size of the padded</summary>
			public int PaddedSize { get; private set; }

			/// <summary>Gets the component count</summary>
			public int ComponentCount { get; private set; }

			/// <summary>Gets or sets the CRC</summary>
			public Int64 CRC {
				get
				{
//					if (!HasValue)
//					{
//						_crcReady.WaitOne();
						if (!HasValue)
							throw new InvalidOperationException("_crc.HasValue = false, after waiting for _crcReady");
//					}
					return _crc.Value;
				}

				internal set
				{
					_crc = value;
				}
			}
			public bool HasValue { get { return _crc.HasValue; } }
			private Int64? _crc;

//			internal EventWaitHandle _crcReady = new EventWaitHandle(false, EventResetMode.ManualReset);

			internal Task _crcTask;

			/// <summary>Initializes a new instance of the <see cref="Artefacts.FileSystem.FileCRC+Region"/> class</summary>
			/// <param name="start">Start.</param>
			/// <param name="finish">Finish.</param>
			public Region(long start, long finish)
			{
				if ((finish - start + 1) > long.MaxValue)
					throw new ArgumentOutOfRangeException("finish", finish, "start = " + start + ", so finish must be <= " + (start + int.MaxValue));
				Start = start;
				Finish = finish;
				Size = (int)(finish - start) + 1;
				for (PaddedSize = Size; PaddedSize % sizeof(Int64) != 0; PaddedSize++) ;
				ComponentCount = PaddedSize / ComponentSize;
			}
		}

		public File File { get; internal set; }
		public FileInfo FileInfo { get; internal set; }

		public Region[] Regions { get; internal set; }

		public Int64 CRC {
			get
			{
				if (!HasCRC)
				{
//					if (_crcTask == null)
//						throw new InvalidOperationException("_crcTask = null");
//					if (_crcTask.Status == TaskStatus.Canceled || _crcTask.Status == TaskStatus.Faulted)// .Status != TaskStatus.Running || _crcTask.Status == TaskStatus.WaitingToRun == null)
//						throw new InvalidOperationException("Couldn't return CRC because _crc.HasValue = false and _crcTask.Status = " + _crcTask.Status);
//					_crcTask.Wait();
//					if (!HasCRC)
//						throw new InvalidOperationException("After waiting for CRC task, _crc.HasValue stil = false");
					_crcReady.WaitOne();
					if (!HasCRC)
						throw new InvalidOperationException("_crc.HasValue = false, after waiting for _crcReady");
				}
				return _crc.Value;
			}
			internal set
			{
				_crc = value;
			}
		}
		private Int64? _crc;
		private Task _crcTask;
		private EventWaitHandle _crcReady = new EventWaitHandle(false, EventResetMode.ManualReset);
		private bool _isQueued = false;

		public bool HasCRC { get { return _crc.HasValue; } }

		public bool IsCRCQueued { get { return _isQueued; } }

		static FileCRC()
		{
			_crcSemaphore = new Semaphore(4, 4);
		}

		public FileCRC()
		{
			FileInfo = null;
			Regions = new Region[0];
		}

		public FileCRC(Int64 crc)
		{
			FileInfo = null;
			Regions = new Region[0];
			CRC = crc;
		}

		public FileCRC(FileInfo fileInfo, IEnumerable<Region> regions = null)
		{
			Init(fileInfo, regions);
		}

		public FileCRC(File file, FileInfo fileInfo, IEnumerable<Region> regions, Action<File> onComplete = null)
		{
			Init(fileInfo, regions);
			Monitor.Enter(QueueMonitorLock);
			try
			{	
				QueueCount++;
				_isQueued = true;
				TaskCreationOptions taskOptions = TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning;
				_crcSemaphore.WaitOne();
				_crcTask = new Task(() => {
					byte[] data;
					try {
						data = new byte[_readBufferSize];

						// TODO: If this works reliably, maybe try async reading for speed boost (would read data in background while calc'ing CRC on loaded data? would need 2x buffers)
						using (FileStream fs = FileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
						{
							long length = fs.Length;
							long position = fs.Position;
							int regionIndex = 0;
							Region region = Regions[regionIndex];			// OK to assume array length is >= 1?
							Int64 crc = Int64.MaxValue;
							while (position < length)
							{
								long remainingBytes = length - position;
								int readTryCount = remainingBytes < (long)_readBufferSize ? (int)remainingBytes : _readBufferSize;
								int readActualCount = fs.Read(data, 0, readTryCount);
								if (readActualCount < 0)
									throw new IOException(string.Format(
										"Error reading \"{0}\", attempted {1} bytes from position {2}",
										FileInfo.Name, readTryCount, position));
								Array.Clear(data, readActualCount, _readBufferSize - readActualCount);
								for (int i = 0; i < _readBufferSize; i += Region.ComponentSize)
								{
									if (position + i + Region.ComponentSize >= region.Finish)
									{
										region.CRC = crc;
										if (++regionIndex >= Regions.Length)
											break;
										region = Regions[regionIndex];
										crc = Int64.MaxValue;
									}
									crc -= BitConverter.ToInt64(data, i);
								}
								position += readActualCount;
							}
						}
						CRC = Int64.MaxValue - Regions.Sum(r => r.CRC);
					}
					catch (Exception ex)
					{
						Log.Error(ex);
					}
					finally
					{
						data = null;
//						_mallocTotal -= new BigInteger((ulong)dataSize);
						QueueCount--;
						_isQueued = false;
						_crcReady.Set();
						_crcTask = null;
						if (onComplete != null)
							onComplete(file);
						Monitor.Exit(QueueMonitorLock);
						GC.Collect();
						_crcSemaphore.Release();
					}
				}, taskOptions);
				_crcTask.Start();
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				QueueCount--;
				_isQueued = false;
				_crcReady.Set();
				if (_crcTask != null)
					_crcTask = null;
				if (onComplete != null)
					onComplete(file);
				Monitor.Exit(QueueMonitorLock);
			}
			finally
			{
				GC.Collect();
			}
		}

		private void Init(FileInfo fileInfo, IEnumerable<Region> regions, File file = null)
		{
			File = file;
			FileInfo = fileInfo;
			Regions = (regions != null && regions.Count() > 0) ? regions.ToArray()
				: new FileCRC.Region[] { new FileCRC.Region(0, FileInfo.Length - 1) };
		}

		public static implicit operator Int64(FileCRC crc)
		{
			return crc._crc.HasValue ? crc._crc.Value : default(Int64);
		}
	}
}


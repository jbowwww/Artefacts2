using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Artefacts.FileSystem
{
	public class FileCRC
	{
		public static int QueueCount = 0;

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
					if (!HasValue)
					{
						_crcReady.WaitOne();
						if (!HasValue)
							throw new InvalidOperationException("_crc.HasValue = false, after waiting for _crcReady");
					}
					return _crc.Value;
				}

				internal set
				{
					_crc = value;
				}
			}
			public bool HasValue { get { return _crc.HasValue; } }
			private Int64? _crc;

			internal EventWaitHandle _crcReady = new EventWaitHandle(false, EventResetMode.ManualReset);

			/// <summary>Initializes a new instance of the <see cref="Artefacts.FileSystem.FileCRC+Region"/> class</summary>
			/// <param name="start">Start.</param>
			/// <param name="finish">Finish.</param>
			public Region(long start, long finish)
			{
				if ((finish - start + 1) > int.MaxValue)
					throw new ArgumentOutOfRangeException("finish", finish, "start = " + start + ", so finish must be <= " + (start + int.MaxValue));
				Start = start;
				Finish = finish;
				Size = (int)(finish - start) + 1;
				for (PaddedSize = Size; PaddedSize % sizeof(Int64) != 0; PaddedSize++) ;
				ComponentCount = PaddedSize / ComponentSize;
			}
		}

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

		public bool HasCRC { get { return _crc.HasValue; } }

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

		public FileCRC(FileInfo fileInfo, IEnumerable<Region> regions, Int64 crc)
		{
			Init(fileInfo, regions);
			CRC = crc;
		}

		public FileCRC(FileInfo fileInfo, IEnumerable<Region> regions, Action onComplete = null)
		{
			Init(fileInfo, regions);
			TaskCreationOptions taskOptions = TaskCreationOptions.LongRunning;
			_crcTask = new Task(() =>
			{
				FileCRC.QueueCount++;
				byte[] data;
				using (FileStream fs = FileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					data = new byte[fs.Length];
					int offset = 0;
					while (fs.Position < fs.Length)
					{
						int readTryCount = fs.Length >= int.MaxValue ? int.MaxValue : (int)fs.Length;
						int readActualCount = fs.Read(data, offset, readTryCount);
						if (readActualCount < readTryCount)
							throw new IOException(
								"File read \"" + FileInfo.Name + "\" offset " + offset +
								", count " + readTryCount + " only read " + readActualCount + " bytes");
						offset += readActualCount;
					}
				}

//				ParallelOptions parallelOptions = new ParallelOptions() {
//					MaxDegreeOfParallelism = 8
//				};
//				Parallel.ForEach(Regions, parallelOptions, (region) =>

				foreach (Region region in Regions)
				{
					region._crcReady.Reset();
					TaskCreationOptions subTaskOptions = TaskCreationOptions.AttachedToParent;
					// TODO: Extract crc calc'ion from byte[] buffer into a Region member method(s) e.g. GetRegionData / CalculateCRC()
					Task calcCrcTask = new Task(() => {
						Int64 crc = 0;
						byte[] regionData =
							new ArraySegment<byte>(data, (int)region.Start, (int)region.Size).Array
								.Concat(new byte[region.PaddedSize - region.Size]).ToArray();
						for (long i = 0; i < region.ComponentCount; i++)
							crc += BitConverter.ToInt64(regionData, (int)i);
						region.CRC = Int64.MaxValue - crc;
						region._crcReady.Set();
					}, subTaskOptions);
					calcCrcTask.Start();
				}
//				);
				FileCRC.QueueCount--;
			}, taskOptions);
			_crcTask.ContinueWith((task) => {
				CRC = Int64.MaxValue - Regions.Sum(r => r.CRC);
				if (onComplete != null)
					onComplete();
			});
			_crcTask.Start();
		}

		private void Init(FileInfo fileInfo, IEnumerable<Region> regions)
		{
			FileInfo = fileInfo;
			Regions = regions.ToArray();
		}

		public static implicit operator Int64(FileCRC crc)
		{
			return crc._crc.HasValue ? crc._crc.Value : default(Int64);
		}
	}
}


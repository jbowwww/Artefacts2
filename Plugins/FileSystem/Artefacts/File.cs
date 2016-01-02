using System;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Diagnostics;
using ServiceStack.Logging;
using Artefacts.Extensions;

namespace Artefacts.FileSystem
{
	/// <summary>
	/// File.
	/// </summary>
	public class File : FileSystemEntry
	{
		#region Static members
		protected static ILog Log = Artefact.Log;

		/// <summary>
		/// Returns a formatted string representing a file size
		/// </summary>
		/// <returns>The formatted size string</returns>
		/// <param name="Size">File size</param>
		public static string FormatSize(long Size)
		{
			string[] units = { "B", "KB", "MB", "GB", "TB" };
			double s = (double)Size;
			int unitIndex = 0;
			while (s > 1024 && unitIndex < units.Length)
			{
				unitIndex++;
				s /= 1024;
			}
			return string.Concat(s.ToString("N2"), units[unitIndex]);
		}
	
		private static bool _CRCCancelThread = false;
		private static int _crcBufferLongCount = 131072;
		private static readonly ConcurrentQueue<File> _crcQueue = new ConcurrentQueue<File>();
		private static Thread _crcThread;
		private static ThreadStart _crcThreadFunc = File.CRCThreadFunc;
		private static void CRCThreadFunc()
		{
			File f;
			while (!Volatile.Read(ref File._CRCCancelThread) && _crcQueue.TryDequeue(out f))
				f.CalculateCRC();
			Volatile.Write<Thread>(ref _crcThread, null);
		}
		public static void CRCAbortThread(bool wait = false)
		{
			_CRCCancelThread = true;
			if (wait)
				CRCWaitThreadFinish();
		}
		public static void CRCWaitThreadFinish()
		{
			while (Volatile.Read<Thread>(ref _crcThread) != null)
				Thread.Sleep(77);
		}
		#endregion

		private bool _crcQueued;
		private readonly EventWaitHandle _crcWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

		#region Public fields & properties
		/// <summary>
		/// The size.
		/// </summary>
		public long Size { get; set; }

		public EventWaitHandle CRCWaitHandle {
			get { return _crcWaitHandle; }
		}

		public long? CRC {
			get;
			set;
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name {
			get { return System.IO.Path.GetFileName(Path); }
			set
			{
//				Debug.Assert(
//					Path.Substring(Path.Length - 1 - value.Length).ToLower().Equals(value),
//					"File.Name.set: value does not match filename component of File.Path",
//					"File.Name.set: {0} does not match filename component of File.Path ({1})",
//					value, Path);
			}
		}

		/// <summary>
		/// Gets the name without extension.
		/// </summary>
		public virtual string NameWithoutExtension {
			get { return System.IO.Path.GetFileNameWithoutExtension(Path); }
			set
			{
//				string filename = Path.Substring(Path.Length - 1 - value.Length);
//				int dotI = filename.LastIndexOf(".");
//				if (dotI >= 0)
//					filename.Substring(0, filename.Length - dotI);
//				Debug.Assert(
//					filename.ToLower().Equals(value),
//					"File.NameWithoutExtension.set: value does not match filename component of File.Path",
//					"File.NameWithoutExtension.set: {0} does not match filename component of File.Path ({1})",
//					value, Path);
			}
		}

		/// <summary>
		/// Gets the extension.
		/// </summary>
		public virtual string Extension {
			get { return System.IO.Path.GetExtension(Path); }
			set
			{
//				int dotI = Path.LastIndexOf(".");
//				if (dotI >= 0)
//				{
//					string extension = Path.Substring(dotI + 1);
//					Debug.Assert(
//						extension.ToLower().Equals(value),
//						"File.Extension.set: value does not match extension component of File.Path",
//						"File.Extension.set: {0} does not match extension component of File.Path ({1})",
//						value, Path);
//				}
			}
		}

		/// <summary>
		/// Gets or sets the file info.
		/// </summary>
		protected virtual FileInfo FileInfo {
			get
			{
				return (FileInfo)base.Info;
			}
			set
			{
				base.SetInfo(value);
				Size = value.Length;
			}
		}
		#endregion

		#region Methods
		#region Construction
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.File"/> class.
		/// </summary>
		public File()
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.File"/> class.
		/// </summary>
		/// <param name="path">Path.</param>
		public File(string path)
		{
			FileInfo = new FileInfo(path);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.File"/> class.
		/// </summary>
		/// <param name="fileInfo">File info.</param>
		public File (FileInfo fileInfo)
		{
			FileInfo = fileInfo;
		}
		#endregion

		/// <summary>
		/// Calculates the CR.
		/// </summary>
		public long CalculateCRC()
		{
			CRC = new long?();
			long finalCRC = long.MaxValue;
			try
			{
				using (FileStream f = new System.IO.FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read, sizeof(long) * _crcBufferLongCount, true))
				       //.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					int fLength = (int)f.Length;
					Task readTask;
					EventWaitHandle waitRead = new EventWaitHandle(false, EventResetMode.ManualReset);
					while (fLength > 0)
					{
						if (File._CRCCancelThread)
							return 0;
						byte[] fileBytes = new byte[sizeof(long) * _crcBufferLongCount];
						waitRead.Reset();
//						int c;
//						c = f.Read(fileBytes, 0, sizeof(long) * 32);
//						if (c == 0)
//							break;
//						fLength -= c;
//						while (c < sizeof(long) * 32)
//							fileBytes[c++] = 0;
//						for (int i = 0; i < 32; i++)
//							crc += BitConverter.ToInt64(fileBytes, sizeof(long) * i);
						 readTask =
							f.ReadAsync(fileBytes, 0, sizeof(long) * _crcBufferLongCount)
							 .ContinueWith((task, state) => {
								waitRead.Set();
								long crc = 0;
								int c = task.Result;
								byte[] fileBytesOp = (byte[])state;
								if (c == 0)
									fLength = 0;
								Volatile.Write(ref fLength, Volatile.Read(ref fLength) - c);	//fLength -= c;
								while (c < sizeof(long) * _crcBufferLongCount)
									fileBytesOp[c++] = 0;
								for (int i = 0; i < _crcBufferLongCount; i++)
									crc += BitConverter.ToInt64(fileBytesOp, sizeof(long) * i);
								Volatile.Write(ref finalCRC, Volatile.Read(ref finalCRC) - crc);
							}, fileBytes);
//						readTask.Start();
						waitRead.WaitOne();
					}
				}
			}
			catch (IOException ex)
			{
				Log.ErrorFormat("f.CalculateCRC() (Path=\"{0}\")\n{1}", Path, ex);
				//throw;
			}
			finally
			{
				CRC = finalCRC;
				CRCWaitHandle.Set();
			}
			return CRC.Value;
		}

		/// <summary>
		/// Queues the calculate CR.
		/// </summary>
		public void QueueCalculateCRC(bool recalculate = false)
		{
			if (!Volatile.Read(ref _CRCCancelThread))
			{
				if (CRC.HasValue && !recalculate)
					CRCWaitHandle.Set();
				else if (!Volatile.Read(ref _crcQueued))
					{
						Volatile.Write(ref _crcQueued, true);
						_crcQueue.Enqueue(this);
						if (Volatile.Read<Thread>(ref _crcThread) == null)
						{
							Volatile.Write<Thread>(ref _crcThread, new Thread(_crcThreadFunc));
							Volatile.Read<Thread>(ref _crcThread).Start();
						}
					}
			}
		}

		/// <summary>
		/// Waits the CR.
		/// </summary>
		/// <remarks>
		/// Does not check that this file has been queued for CRC calculation - caller must ensure it is so
		/// </remarks>
		/// <returns><c>true</c> if waiting was required, <c>false</c> if not (CRC already calculated)</returns>
		public bool WaitCRC()
		{
			if (!CRC.HasValue)
			{
				if (!CRCWaitHandle.WaitOne())
					throw new InvalidOperationException("File.CRCWaitHandle.WaitOne() returned false");
				return true;
			}
			return false;
		}

		/// <summary>
		/// Queues the wait calculate CR.
		/// </summary>
		/// <returns>The CRC</returns>
		public long QueueWaitCalculateCRC(bool recalculate = false)
		{
			QueueCalculateCRC(false);
			WaitCRC();
			if (!CRC.HasValue)
				throw new InvalidDataException("File.CRC does not have value");
			return CRC.Value;
		}

		/// <summary>
		/// Inners the equals.
		/// </summary>
		/// <returns>true</returns>
		/// <c>false</c>
		/// <param name="obj">Object.</param>
		protected override bool InnerEquals(object obj)
		{
			return typeof(File).IsAssignableFrom(obj.GetType())
				&& Size == ((File)obj).Size  && base.InnerEquals(obj);
		}
		#endregion
	}
}

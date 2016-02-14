using System;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ServiceStack.Logging;
using Artefacts.Extensions;
using System.Linq;

namespace Artefacts.FileSystem
{
	/// <summary>
	/// File.
	/// </summary>
	public class File : FileSystemEntry
	{
		#region Static members
		protected static ILog Log = Artefact.Log;

		// TODO: Static configuration settings properties/fields?
		// e.g. for configuring how/when CRCs are calculated, etc

//		public struct Size
//		{
//			public static long Byte = 1;
//			public static long KiloByte = 1024;
//			public static long MegaByte = 1024 * 1024;
//			public static long GigaByte = 1024 * 1024 * 1024;
////			public static long TeraByte = 1024 * 1024 * 1024 * 1024;
////			public static long PetaByte = 1024 * 1024 * 1024 * 1024 * 1024;
//			// ..?
//			long _instanceSize;
//			public Size(long size)
//			{
//				_instanceSize = size;
//			}
//			public string Format()
//			{
//				return File.FormatSize(_instanceSize);
//			}
//			public static implicit operator long(Size size)
//			{
//				return size._instanceSize;
//			}
//		};

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
	
//		private static bool _CRCCancelThread = false;
//		private static int _crcBufferLongCount = 1048576;		// number of long's to read at a time
//		private static readonly ConcurrentQueue<File> _crcQueue = new ConcurrentQueue<File>();
//		private static Thread _crcThread;
//		private static ThreadStart _crcThreadFunc = File.CRCThreadFunc;
//		private static void CRCThreadFunc()
//		{
//			File f;
//			while (!Volatile.Read(ref File._CRCCancelThread) && _crcQueue.TryDequeue(out f))
//				f.CalculateCRC();
//			Volatile.Write<Thread>(ref _crcThread, null);
//		}
//		public static void CRCAbortThread(bool wait = false)
//		{
//			_CRCCancelThread = true;
//			if (wait)
//				CRCWaitThreadFinish();
//		}
//		public static void CRCWaitThreadFinish()
//		{
//			while (Volatile.Read<Thread>(ref _crcThread) != null)
//				Thread.Sleep(77);
//		}
//		public static int CRCQueueCount {
//			get { return _crcQueue.Count; }
//		}
		#endregion

//		private bool _crcQueued;
//		private readonly EventWaitHandle _crcWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

		#region Public fields & properties
		/// <summary>
		/// The size.
		/// </summary>
		public long Size { get; set; }
//
//		public EventWaitHandle CRCWaitHandle {
//			get { return _crcWaitHandle; }
//		}


		private FileCRC _crc;
		public Int64? CRC {
			get
			{
				if (!HasCRC)
					return null;
//					throw new InvalidOperationException("Couldn't return CRC because _crc.HasValue = false");
				return _crc.CRC;
			}
			set
			{
				_crc = (value != null && value.Value != 0) ? new FileCRC(value.Value) : new FileCRC();//new FileInfo(Path));//value.Value);
//				_crc.CRC = value;
			}
		}

		public string CRCString {
			get
			{
				return "" + _crc.Regions.Length + ":" + string.Join(":", _crc.Regions.Select(region => region.CRC.ToHex()));
			}
			set
			{

			}
		}

		public bool HasCRC { get { return _crc.HasCRC; } }// CRC != 0 && CRC.HasValue && CRC.Value != 0; } }

		public bool IsCRCQueued { get { return _crc.IsCRCQueued; } }	///*!HasCRC*/ /* && _crcReady*/; } }

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
		public File() {}
		public File(string path)
		{
			FileInfo = new FileInfo(path);
			_crc = new FileCRC(FileInfo, new FileCRC.Region[] { new FileCRC.Region(0, FileInfo.Length - 1) });
		}
		public File (FileInfo fileInfo)
		{
			FileInfo = fileInfo;
			_crc = new FileCRC(FileInfo, new FileCRC.Region[] { new FileCRC.Region(0, FileInfo.Length - 1) });
		}
		#endregion

		#region CRC

		public long GetCRC(bool force = false)
		{
			DoCRC(force, true);
			if (!HasCRC)
				throw new InvalidOperationException("GetCRC(force=" + force + ") waited and then CRC failed");
			return CRC.Value;
		}


		public void DoCRC(bool force = false, bool wait = false, Action<File> continueWith = null)
		{
			EventWaitHandle waitCRC = new EventWaitHandle(false, EventResetMode.ManualReset);
			if (force || (!HasCRC && !IsCRCQueued))
			{
				_crc = new FileCRC(
					FileInfo ?? (FileInfo = new FileInfo(Path)),
					new FileCRC.Region[] { new FileCRC.Region(0, FileInfo.Length - 1) },
					() => waitCRC.Set());
				if (wait && IsCRCQueued)
					waitCRC.WaitOne();
				if (continueWith != null)
					continueWith(this);
			}
		}

		// TODO: Multisection CRC?

		/// <summary>
		/// Calculates the CRC
		/// </summary>
//		public long? CalculateCRC()
//		{
//			long finalCRC = long.MaxValue;
//			try
//			{
//				Task readTask = null;
//				using (FileStream f = new System.IO.FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read, sizeof(long) * _crcBufferLongCount, true))
//				       //.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read))
//				{
//					int fLength = (int)f.Length;
//					EventWaitHandle waitRead = new EventWaitHandle(false, EventResetMode.ManualReset);
//					while (fLength > 0)
//					{
//						if (File._CRCCancelThread)
//							return null;		// finally block at end should still signal wait handle (??)
//						byte[] fileBytes = new byte[sizeof(long) * _crcBufferLongCount];
//						waitRead.Reset();
////						int c;
////						c = f.Read(fileBytes, 0, sizeof(long) * 32);
////						if (c == 0)
////							break;
////						fLength -= c;
////						while (c < sizeof(long) * 32)
////							fileBytes[c++] = 0;
////						for (int i = 0; i < 32; i++)
////							crc += BitConverter.ToInt64(fileBytes, sizeof(long) * i);
//						 readTask =
//							f.ReadAsync(fileBytes, 0, sizeof(long) * _crcBufferLongCount)
//							 .ContinueWith((task, state) => {
//								waitRead.Set();
//								long crc = 0;
//								int c = task.Result;
//								byte[] fileBytesOp = (byte[])state;
//								if (c == 0)
//									fLength = 0;
//								Volatile.Write(ref fLength, Volatile.Read(ref fLength) - c);	//fLength -= c;
//								while (c < sizeof(long) * _crcBufferLongCount)
//									fileBytesOp[c++] = 0;
//								for (int i = 0; i < _crcBufferLongCount; i++)
//									crc += BitConverter.ToInt64(fileBytesOp, sizeof(long) * i);
//								Volatile.Write(ref finalCRC, Volatile.Read(ref finalCRC) - crc);
//							}, fileBytes);
////						readTask.Start();
//						waitRead.WaitOne();
//					}
//				}
//				if (readTask != null)
//					readTask.Wait();
//				CRC = finalCRC;
//			}
//			catch (IOException ex)
//			{
//				Log.ErrorFormat("f.CalculateCRC() (Path=\"{0}\")\n{1}", Path, ex);
//				//throw;
//			}
//			finally
//			{
//				//CRC = finalCRC;
//				CRCWaitHandle.Set();
//				Volatile.Write(ref _crcQueued, false);
//			}
//			return CRC;
//		}

		/// <summary>
		/// Queues the calculate CR.
		/// </summary>
//		public void QueueCalculateCRC(bool recalculate = false)
//		{
//			if (!Volatile.Read(ref _CRCCancelThread))
//			{
//				if (CRC.HasValue && !recalculate)
//					CRCWaitHandle.Set();
//				else if (!Volatile.Read(ref _crcQueued))
//				{
//					Volatile.Write(ref _crcQueued, true);
//					CRC = new long?();
//					_crcQueue.Enqueue(this);
//					CRCWaitHandle.Reset();
//					if (Volatile.Read<Thread>(ref _crcThread) == null)
//					{
//					Volatile.Write<Thread>(ref _crcThread, new Thread(_crcThreadFunc) { Priority = ThreadPriority.AboveNormal });
//						Volatile.Read<Thread>(ref _crcThread).Start();
//					}
//				}
//			}
//		}

		/// <summary>
		/// Waits the CR.
		/// </summary>
		/// <remarks>
		/// Does not check that this file has been queued for CRC calculation - caller must ensure it is so
		/// </remarks>
		/// <returns><c>true</c> if CRC has a value set, <c>false</c> if not</returns>
//		public bool WaitCRC()
//		{
//			if (!CRC.HasValue)
//			{
//				if (!CRCWaitHandle.WaitOne())
//					throw new InvalidOperationException("File.CRCWaitHandle.WaitOne() returned false");
//				return CRC.HasValue;
//			}
//			return true;
//		}

		/// <summary>
		/// Queues the wait calculate CR.
		/// </summary>
		/// <returns>The CRC</returns>
//		public long? QueueWaitCalculateCRC(bool recalculate = false)
//		{
//			QueueCalculateCRC(false);
//			WaitCRC();
//			return CRC;
//		}
		#endregion

		public override bool Exists() { return System.IO.File.Exists(Path); }

		public override string ToString()
		{
			return string.Format(
				"[File: Attributes={0} CreationTime={1} LastAccessTime={2} LastWriteTime={3} Size={4} CRC={5} HasCRC={6} Extension={7} Path={8}]",
				Attributes, CreationTime, LastAccessTime, LastWriteTime, Size, CRC, HasCRC, Extension, Path);
		}
		#endregion
	}
}

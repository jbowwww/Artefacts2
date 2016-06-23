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
		#endregion

		#region Public fields & properties
		/// <summary>
		/// The size.
		/// </summary>
		public long Size { get; set; }

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
			DoCRC(force);
			if (!HasCRC)
				throw new InvalidOperationException("GetCRC(force=" + force + ") waited and then CRC failed");
			return CRC.Value;
		}

		public void DoCRC(bool force = false, Action<File> continueWith = null)
		{
			EventWaitHandle waitCRC = new EventWaitHandle(false, EventResetMode.ManualReset);
			if (force || (!HasCRC && !IsCRCQueued))
				_crc = new FileCRC(this,
					FileInfo != null ? FileInfo : (FileInfo = new FileInfo(Path)),
					new FileCRC.Region[] { new FileCRC.Region(0, FileInfo.Length - 1) },
					continueWith);
		}
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

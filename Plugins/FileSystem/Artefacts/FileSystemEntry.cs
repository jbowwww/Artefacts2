using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Artefacts.FileSystem
{
	/// <summary>
	/// File system entry.
	/// </summary>
	public abstract class FileSystemEntry
	{
		#region Private fields
		/// <summary>
		/// The <see cref="System.IO.FileSystemInfo"> for this <see cref="FileSystemEntry"/> 
		/// </summary>
		private System.IO.FileSystemInfo _fileSystemInfo;
		#endregion

		#region Public fields & properties
		/// <summary>
		/// The <see cref="System.IO.FileSystemInfo"> for this <see cref="FileSystemEntry"></param>
		/// </summary>
		protected System.IO.FileSystemInfo Info {
			get
			{
//				if (_fileSystemInfo == null)
//					throw new NullReferenceException("this._fileSystemInfo == null");
				return _fileSystemInfo;
			}
			set
			{
				SetInfo(value);
			}
		}

		/// <summary>
		/// The path.
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// Gets or sets the depth of the path (i.e. number of '/' or '\\')
		/// </summary>
		/// <value>The path depth.</value>
		public int? PathDepth {
			get
			{
				return Path.Count(c => c == '/' || c == '\\');
			}
			set
			{
				// ?? Haven't figured best way to approach this sort of thing
			}
		}

		/// <summary>
		/// Gets or sets the deleted.
		/// </summary>
		public bool? Deleted {
			get
			{
				return !this.Exists();
			}
			set
			{
				// ??
			}
		}

		/// <summary>
		/// The drive.
		/// </summary>
		public Drive Drive { get; private set; }

		/// <summary>
		/// Gets or sets the drive parition.
		/// </summary>
		public string DriveParition {
			get { return Drive == null ? string.Empty : Drive.Partition; }
			set { Drive = Drive.All.SingleOrDefault(drive => drive.Partition == value); }
		}

		/// <summary>
		/// Gets or sets the directory.
		/// </summary>
		[IgnoreDataMember]
		public Directory Directory { get; set; }

		/// <summary>
		/// Gets or sets the directory identifier.
		/// </summary>
		/// <value>The directory identifier.</value>
		public string DirectoryPath { get; set; }
//			get { return Directory == null ? string.Empty : Directory.Path; }
//			set { Directory = (Directory)Directory.All.FromPath(value); } //Directory = new Directory(value); }
//		}

		/// <summary>
		/// The attributes.
		/// </summary>
		public FileAttributes Attributes { get; set; }

		/// <summary>
		/// Gets or sets the creation time.
		/// </summary>
		public DateTime CreationTime { get; set; }

		/// <summary>
		/// Gets or sets the access time.
		/// </summary>
		public DateTime LastAccessTime { get; set; }

		/// <summary>
		/// Gets or sets the modify time.
		/// </summary>
		public DateTime LastWriteTime { get; set; }
		#endregion

		#region Methods
		#region Construction
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.FileSystemEntry"/> class.
		/// </summary>
		/// <remarks>
		/// Only intended to provide a default constructor for serialization purposes, if needed by any serialzation schemes
		/// </remarks>
		public FileSystemEntry()
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.FileSystemEntry"/> class.
		/// </summary>
		/// <param name="fileSystemInfo"><see cref="System.IO.FileSystemInfo"> for this <see cref="FileSystemEntry"></param>
		public FileSystemEntry(System.IO.FileSystemInfo fileSystemInfo)
		{
			SetInfo(fileSystemInfo);
		}
		#endregion

		#region System.Object overrides
		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="Artefacts.FileSystem.FileSystemEntry"/>.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with the current <see cref="Artefacts.FileSystem.FileSystemEntry"/>.</param>
		/// <returns>
		/// <c>true</c> if the specified <see cref="System.Object"/> is equal to the current
		/// <see cref="Artefacts.FileSystem.FileSystemEntry"/>; otherwise, <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (object.ReferenceEquals(this, obj))
				return true;
			return InnerEquals(obj);
		}

		/// <summary>
		/// Serves as a hash function for a <see cref="Artefacts.FileSystem.FileSystemEntry"/> object.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode()
		{
			return this.Path.GetHashCode() + ( Drive == null ? 0 : Drive.GetHashCode());
		}
		#endregion

		#region Implementation
		/// <summary>
		/// Inners the equals.
		/// </summary>
		/// <returns><c>true</c>, if equals was innered, <c>false</c> otherwise.</returns>
		/// <param name="obj">Object.</param>
		protected virtual bool InnerEquals(object obj)
		{
			FileSystemEntry fse = (FileSystemEntry)obj;
			if (fse == null)
				return false;
			return this.Path.Equals(fse.Path);
		}

		/// <summary>
		/// Sets the info.
		/// </summary>
		/// <param name="value">Value.</param>
		protected void SetInfo(System.IO.FileSystemInfo value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			_fileSystemInfo = value;
			this.Path = _fileSystemInfo.FullName;
			Attributes = _fileSystemInfo.Attributes;
			CreationTime = _fileSystemInfo.CreationTime;
			LastAccessTime = _fileSystemInfo.LastAccessTime;
			LastWriteTime = _fileSystemInfo.LastWriteTime;
			Drive = Drive.All.FromPath(Path);
			Directory = null;//(Directory)Directory.All.FromPath(Path);
		}

		/// <summary>
		/// Convenience override of System.IO.File/Directory.Exists()
		/// Considered making it a property but then it's messy to get the artefacts system to ignore it
		/// Actually could also replace 'Deleted' member as it performs the same purpose ... decisions??
		/// Actually that's what I'm gonna do, for now at least it cleans up the test client code a bit
		/// which may be a hint that it's the way to go
		/// </summary>
		public abstract bool Exists();
		#endregion
		#endregion
	}
}

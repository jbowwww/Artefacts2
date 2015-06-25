using System;
using System.Collections.Generic;

namespace Artefacts.FileSystem
{
	/// <summary>
	/// Directory.
	/// </summary>
	public class Directory : FileSystemEntry
	{
		#region Static members
		/// <summary>
		/// Gets all.
		/// </summary>
		/// <value>All.</value>
		public static IEnumerable<Directory> All {
			get { return _all != null ? _all : _all = new List<Directory>(); }
		}
		private static IEnumerable<Directory> _all = null;
		#endregion

		#region Public fields & properties
		/// <summary>
		/// Gets or sets the directory info.
		/// </summary>
		/// <value>The directory info.</value>
		public virtual System.IO.DirectoryInfo DirectoryInfo {
			get { return (System.IO.DirectoryInfo)base.Info; }
			set
			{
				base.SetInfo(value);
			}
		}
		#endregion

		#region Methods
		#region Construction
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.Directory"/> class.
		/// </summary>
		public Directory()
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.Directory"/> class.
		/// </summary>
		/// <param name="path">Path.</param>
		public Directory(string path)
		{
			DirectoryInfo = new System.IO.DirectoryInfo(path);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.Directory"/> class.
		/// </summary>
		/// <param name="directoryInfo">Directory info.</param>
		public Directory(System.IO.DirectoryInfo directoryInfo)
		{
			DirectoryInfo = directoryInfo;
		}
		#endregion

		#region Implementation

		#endregion
		#endregion
	}
}

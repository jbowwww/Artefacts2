using System;
using System.IO;

namespace Artefacts.FileSystem
{
	/// <summary>
	/// File.
	/// </summary>
	public class File : FileSystemEntry
	{
		#region Public fields & properties
		/// <summary>
		/// The size.
		/// </summary>
		public long Size { get; set; }

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name {
			get { return System.IO.Path.GetFileName(Path); }
		}

		/// <summary>
		/// Gets the name without extension.
		/// </summary>
		public virtual string NameWithoutExtension {
			get { return System.IO.Path.GetFileNameWithoutExtension(Path); }
		}

		/// <summary>
		/// Gets the extension.
		/// </summary>
		public virtual string Extension {
			get { return System.IO.Path.GetExtension(Path); }
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

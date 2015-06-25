using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.ServiceModel;

using Artefacts.Services;

namespace Artefacts.FileSystem
{
	public class FileSystemArtefactCreator : CreatorBase
	{
		#region Private fields
		private IRepository<Artefact> _repoProxy = null;
		private int recursionDepth;
		#endregion
		
		#region Properties
		public int RecursionLimit;
		public Uri BaseUri;
		public string Pathmatch {
			get { return BaseUri.Query; }
			set
			{
				BaseUri = BaseUri == null ?
					new UriBuilder(Uri.UriSchemeFile, "localhost", 0, "", "").Uri		//	Uri.EscapeDataString("*")
				: new UriBuilder(BaseUri.Scheme, BaseUri.Host, 0, BaseUri.LocalPath, value).Uri;
			}
		}
		#endregion
		
		public FileSystemArtefactCreator(IRepository<Artefact> repoProxy)
		{
			_repoProxy = repoProxy;
			BaseUri = new UriBuilder(Uri.UriSchemeFile, "localhost", 0, "", "").Uri;		//Uri.EscapeDataString("*")
			RecursionLimit = -1;
		}

		#region implemented abstract members of Artefacts.CreatorBase
		public override void Run(object param)
		{
			DriveInfo[] driveInfos = DriveInfo.GetDrives();
			Drive[] drives = new Drive[driveInfos.Length];
				
			for (int i = 0; i < driveInfos.Length; i++)
			{
				string vLabel = driveInfos[i].VolumeLabel;
				Queryable<Artefact> q =
					(Queryable<Artefact>)(
					from a in _repoProxy.Artefacts//.AsEnumerable()
					where /*a.GetType() == typeof(Drive)*/ a is Drive && (a as Drive).Label == vLabel
					select a);
//				drives[i] = (Drive)q.FirstOrDefault();
//				if (q.TotalCount > 0)
				if (q.Count() > 0)
				drives[i] = q.ToArray()[0] as Drive;
				if (drives[i] == null)
					_repoProxy.Add(drives[i] = new Drive(driveInfos[i]));
				else if (drives[i].UpdateAge > TimeSpan.FromMinutes(1))
					_repoProxy.Update(drives[i].Update(driveInfos[i]));				
			}

			recursionDepth = -1;
			Queue<Uri> subDirectories = new Queue<Uri>(new Uri[] { BaseUri });
			while (subDirectories.Count > 0)
			{
				Uri currentUri = subDirectories.Dequeue();
				Drive drive = null;
				foreach (Drive d in drives)
					if (currentUri.LocalPath.StartsWith(d.Label))
						drive = d;

				foreach (string relPath in EnumerateFiles(currentUri))
				{
					string absPath = Path.Combine(currentUri.LocalPath, relPath);
					System.Linq.IQueryable<Artefact> r =
						from a in _repoProxy.Artefacts
						where a is File && (a as File).Path == absPath
						select a;
					File file = (File)r.AsEnumerable().FirstOrDefault();
					if (file == null)
						_repoProxy.Add(new File(new System.IO.FileInfo(absPath), drive));
					else if (file.UpdateAge > TimeSpan.FromMinutes(1))
						_repoProxy.Update(new File(new System.IO.FileInfo(absPath), drive) { Id = file.Id, TimeCreated = file.TimeCreated });
				}

				if (RecursionLimit < 0 || ++recursionDepth < RecursionLimit)
				{
					foreach (string relPath in EnumerateDirectories(currentUri))
					{
						string absPath = Path.Combine(currentUri.LocalPath, relPath);
						var r = //	System.Linq.IQueryable<Artefact>
							from a in _repoProxy.Artefacts																								//.AsEnumerable()
							where a is Directory && (a as Directory).Path == absPath
							select a;
						Directory dir = (Directory)r.AsEnumerable().FirstOrDefault();
						if (dir == null)
							_repoProxy.Add(new Directory(new System.IO.DirectoryInfo(absPath), drive));
						else if (dir.UpdateAge > TimeSpan.FromMinutes(1))
							_repoProxy.Update(new Directory(new System.IO.DirectoryInfo(absPath), drive) { Id = dir.Id, TimeCreated = dir.TimeCreated });
						subDirectories.Enqueue(new Uri(currentUri, relPath));
					}
				}
			}
		}
		#endregion
		
		#region Overridable file system entry  (files and directories) enumerators
		public virtual IEnumerable<string> EnumerateFiles(Uri uri)
		{
			if (!uri.IsFile)
				throw new NotImplementedException("Only URIs with a file schema are currently supported");
			return System.IO.Directory.EnumerateFiles(uri.LocalPath, "*", SearchOption.TopDirectoryOnly);
		}

		public virtual IEnumerable<string> EnumerateDirectories(Uri uri)
		{
			if (!uri.IsFile)
				throw new NotImplementedException("Only URIs with a file schema are currently supported");
			return System.IO.Directory.EnumerateDirectories(uri.LocalPath, "*", SearchOption.TopDirectoryOnly);
		}
		#endregion
	}
}


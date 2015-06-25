using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.ServiceModel;
using System.Reflection;
using Artefacts;
using System.Diagnostics;
using MongoDB.Bson;
using ServiceStack.Logging;
using ServiceStack;
using System.Runtime.Serialization;

namespace Artefacts.FileSystem
{
	/// <summary>
	/// File system agent.
	/// </summary>
	[DataContract]
	public class FileSystemAgent : ClientAgent
	{
		#region Static members
		/// <summary>
		/// The log.
		/// </summary>
		private static IDebugLog Log = new DebugLog<ConsoleLogger>(typeof(Artefact)) { SourceClass = typeof(FileSystemAgent) };
		#endregion

		#region Properties & fields
		IServiceClient _client;

		[DataMember]
		public int RecursionLimit = -1;

		[DataMember]
		public Uri BaseUri { get; private set; }

		[DataMember]
		public string BasePath {
			get { return BaseUri.LocalPath; }
			set { BaseUri = new UriBuilder(BaseUri.Scheme, BaseUri.Host, BaseUri.Port, value, BaseUri.Query).Uri; }
		}

		[DataMember]
		public string Match {
			get { return BaseUri.Query; }
			set { BaseUri = new UriBuilder(BaseUri.Scheme, BaseUri.Host, BaseUri.Port, BaseUri.LocalPath, value).Uri; }
		}

		public IQueryable<FileSystemEntry> FileEntries;

		public IQueryable<File> Files;

		public IQueryable<Directory> Directories;

		public IQueryable<Drive> Drives;
		
//		public IQueryable<FileSystemEntry> FileEntries {
//			get { return (IQueryable<FileSystemEntry>)Repository.Queryables[typeof(FileSystemEntry)]; }
//			private set { Repository.Queryables[typeof(FileSystemEntry)] = value; }
//		}
//		public IQueryable<File> Files {
//			get { return (IQueryable<File>)Repository.Queryables[typeof(File)]; }
//			private set { Repository.Queryables[typeof(File)] = value; }
//		}
//		public IQueryable<Directory> Directories {
//			get { return (IQueryable<Directory>)Repository.Queryables[typeof(Directory)]; }
//			private set { Repository.Queryables[typeof(Directory)] = value; }
//		}
//		public IQueryable<Drive> Drives {
//			get { return (IQueryable<Drive>)Repository.Queryables[typeof(Drive)]; }
//			private set { Repository.Queryables[typeof(Drive)] = value; }
//		}
//  {
//			get { return (IQueryable<Disk>)Repository.Queryables[typeof(Disk)]; }
//			private set { Repository.Queryables[typeof(Disk)] = value; }
//		}
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.FileSystemArtefactCreator"/> class.
		/// </summary>
		/// <param name="repository">Repository.</param>
		public FileSystemAgent(IArtefactsRepository client) : base(client)
		{
			BaseUri = new UriBuilder(Uri.UriSchemeFile, "localhost", 0, "/", "?*").Uri;
			Log.Debug(this.SerializeToString());
		}

		/// <summary>
		/// Run the specified param.
		/// </summary>
		/// <param name="param">Parameter.</param>
		/// <remarks>Artefacts.CreatorBase implementation</remarks>
		public override void Run(object param)
		{
			Log.Debug("Client: " + Client);
			Log.Debug("Host: " + Host.Current.SerializeToString());
			foreach (Disk disk in Disk.Disks)
				Log.Debug("Disk: " + disk.SerializeToString());

			foreach (Disk disk in Disk.Disks)
			{
				Artefact artefact = new Artefact(disk);
				Log.Debug("Artefact: " + artefact.SerializeToString());
				Client.Create(artefact);
			}

//				Disk dbDisk = Disks.FirstOrDefault((d) => disk.Serial.ToLower().CompareTo(d.Serial.ToLower()) == 0);
//				if (dbDisk == null)
//					Repository.Add(disk);
//				else
//				{
//					dbDisk.CopyMembersFrom(disk);
//					Repository.Update(dbDisk.Update());
//				}
//				int recursionDepth = -1;
//			Drive drive;
//			Queue<Uri> subDirectories = new Queue<Uri>(new Uri[] { BaseUri });
//			Uri currentUri;
//			string absPath;
//			
//			// Recurse subdirectories
//			while (subDirectories.Count > 0)
//			{
//				currentUri = subDirectories.Dequeue();
//				drive = Drives.FirstOrDefault((dr) => currentUri.LocalPath.StartsWith(dr.Label));
//
//				foreach (string relPath in EnumerateFiles(currentUri))
//				{
//					absPath = Path.Combine(currentUri.LocalPath, relPath);
//					File file = Files.Where((f) => f.Path.Equals(absPath)).FirstOrDefault();
//					if (file == null)
//						Repository.Add(new File(absPath));
//					else
//						Repository.Update(file.Update());
//				}
//
//				if (RecursionLimit < 0 || ++recursionDepth < RecursionLimit)
//				{
//					foreach (string relPath in EnumerateDirectories(currentUri))
//					{
//						absPath = Path.Combine(currentUri.LocalPath, relPath);
//						Directory dir = Directories.Where((d) => d.Path.Equals(absPath)).FirstOrDefault();
//						if (dir == null)
//							Repository.Add(new Directory(new System.IO.DirectoryInfo(absPath)));
//						else if (dir.UpdateAge > TimeSpan.FromMinutes(1))
//							Repository.Update(dir.Update());
//								new Directory(new System.IO.DirectoryInfo(absPath))
//								{
//									Id = dir.Id,
//									TimeCreated = dir.TimeCreated
//								});
//						subDirectories.Enqueue(new Uri(currentUri, relPath));
//					}
//				}
//			}
		}

		#region Overridable file system entry  (files and directories) enumerators
//		public virtual IEnumerable<Disk> EnumerateDisks(Host host)
//		{
//				List<Disk> disks = new List<Disk>();
//				Process lsblkProcess = Process.Start(
//						new ProcessStartInfo("lsblk")
//						{
//								RedirectStandardOutput = true,
//								RedirectStandardError = true,
//								UseShellExecute = false,
//						});
//				lsblkProcess.WaitForExit(1600);
//				string lsblkOutput;
//				while (!string.IsNullOrEmpty(lsblkOutput = lsblkProcess.StandardOutput.ReadLine()))
//				{
//						string[] tokens = lsblkOutput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
//						if (lsblkOutput.Contains("disk"))
//							disks.Add(new Disk(tokens[0], host));
//				}
//				return disks;
//		}

		/// <summary>
		/// Enumerates the directories.
		/// </summary>
		/// <returns>The directories.</returns>
		/// <param name="uri">URI.</param>
		public virtual IEnumerable<string> EnumerateDirectories(Uri uri)
		{
			if (!uri.IsFile)
				throw new NotImplementedException("Only URIs with a file schema are currently supported");
			return System.IO.Directory.EnumerateDirectories(uri.LocalPath, "*", SearchOption.TopDirectoryOnly);
		}

		/// <summary>
		/// Enumerates the files.
		/// </summary>
		/// <returns>The files.</returns>
		/// <param name="uri">URI.</param>
		public virtual IEnumerable<string> EnumerateFiles(Uri uri)
		{
			if (!uri.IsFile)
				throw new NotImplementedException("Only URIs with a file schema are currently supported");
			return System.IO.Directory.EnumerateFiles(uri.LocalPath, "*", SearchOption.TopDirectoryOnly);
		}
		#endregion
	}
}


using System;
using System.Collections.Generic;
using System.IO;
using Artefacts;

namespace Artefacts.FileSystem
{
	/// <summary>
	/// File system agent.
	/// </summary>
	public class FileSystemAgent : ClientAgent
	{
		#region Fields & properties
		public int RecursionLimit = -1;

		public Uri BaseUri { get; private set; }

		public string BasePath {
			get { return BaseUri.LocalPath; }
			set { BaseUri = new UriBuilder(BaseUri.Scheme, BaseUri.Host, BaseUri.Port, value, BaseUri.Query).Uri; }
		}

		public string Match {
			get { return BaseUri.Query; }
			set { BaseUri = new UriBuilder(BaseUri.Scheme, BaseUri.Host, BaseUri.Port, BaseUri.LocalPath, value).Uri; }
		}

		public IQueryable<FileSystemEntry> FileEntries;

		public IQueryable<File> Files;

		public IQueryable<Directory> Directories;

		public IQueryable<Drive> Drives;

		public IQueryable<Disk> Disks;
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.FileSystemArtefactCreator"/> class.
		/// </summary>
		/// <param name="repository">Repository.</param>
		public FileSystemAgent(IDataStore client, Uri baseUri = null)
			: base(client)
		{
			if (client == null)
				throw new ArgumentNullException("client");
			BaseUri = baseUri ?? new UriBuilder(Uri.UriSchemeFile, "localhost", 0, "/", "").Uri;
			Log.Info(this);
		}

		/// <summary>
		/// Run the specified param.
		/// </summary>
		/// <param name="param">Parameter.</param>
		/// <remarks>Artefacts.CreatorBase implementation</remarks>
		public override void Run(object param)
		{
			MarkStart();

			Log.DebugVariable("Drive.PartitionMountPaths", Drive.PartitionMountPaths);

			UpdateDisks();
			UpdateDrives();

//			IEnumerable<Artefact> dbDirectories = Client.Artefacts.Where(artefact => artefact.Aspects.Any(aspect => aspect.TypeName.Equals("Artefacts.FileSystem.Directory")));
//			IEnumerable<Artefact> dbFiles = Client.Artefacts.Where(artefact => artefact.Aspects.Any(aspect => aspect.TypeName.Equals("Artefacts.FileSystem.File")));

			IRepository<FileSystem.Directory> dbDirectories = Client.GetRepository<FileSystem.Directory>();
			IRepository<FileSystem.File> dbFiles = Client.GetRepository<FileSystem.File>();

			List<string> directoryQueue = new List<string>();
			directoryQueue.Add(BaseUri.LocalPath);

			while (!directoryQueue.IsEmpty())
			{
				string[] queue = directoryQueue.ToArray();
				directoryQueue.Clear();
				foreach (string directoryPath in queue)
				{
					FileSystem.Directory dbDirectory = dbDirectories.SingleOrDefault(dir => dir.Path == directoryPath);
					if (dbDirectory != null)
						dbDirectories.Update(dbDirectory);
					else
						dbDirectories.Create(new Directory(directoryPath));
					directoryQueue.AddRange(EnumerateDirectories(directoryPath));

					foreach (string filePath in EnumerateFiles(directoryPath))
					{
						FileSystem.File dbFile = dbFiles.SingleOrDefault(file => file.Path == filePath);
						if (dbFile != null)
							dbFiles.Update(dbFile);
						else
							dbFiles.Create(new FileSystem.File(filePath));
					}
				}
			}
		}

		#region Overridable file system entry (files and directories) enumerators
		/// <summary>
		/// Enumerates the disks.
		/// </summary>
		protected virtual void UpdateDisks()
		{
			IEnumerable<Artefact> dbDisks = Client.Artefacts.Where(artefact => artefact.Aspects.Any(aspect => aspect.TypeName.Equals("Artefacts.FileSystem.Disk")));
			Log.DebugVariable("Database Disks", dbDisks);
			Log.DebugVariable("Detected Disks", Disk.Disks);
			foreach (Disk disk in Disk.Disks)
			{
				Artefact a = dbDisks.FirstOrDefault(artefact => artefact.Aspects[typeof(Disk)].As<FileSystem.Disk>().Serial == disk.Serial);
				if (a != null)
				{
					Disk aDbDisk = a.Aspects[typeof(Disk)].As<Disk>();
					//					Client.Artefacts.Update(Client.Artefacts.Single(artefact => artefact.Aspects.Contains(aDbDisk)));
//					Log.Info("Disk already exists in DB, would update here");
					Log.InfoVariable("aDbDisk", aDbDisk);
				}
				else
				{
					Artefact artefact = new Artefact(new Uri("disk://" + disk.Serial), disk);
					Client.Artefacts.Create(artefact);
//					Log.Info("Disk created in DB");
					Log.InfoVariable("newDisk", disk);
				}
			}
		}

		/// <summary>
		/// Enumerates the disks.
		/// </summary>
		protected virtual void UpdateDrives()
		{
			IEnumerable<Artefact> dbDrives = Client.Artefacts.Where(artefact => artefact.Aspects.Any(aspect => aspect.TypeName.Equals("Artefacts.FileSystem.Drive")));
			Log.DebugVariable("Database Drives", dbDrives);
			Log.DebugVariable("Detected Drives", Drive.GetDrives());//.All);
			foreach (Drive drive in Drive.All)
			{
				Artefact a = dbDrives.FirstOrDefault(artefact => artefact.Aspects[typeof(Drive)].As<FileSystem.Drive>().Partition == drive.Partition);
				if (a != null)
				{
					Drive aDbDrive = a.Aspects[typeof(Drive)].As<Drive>();
					//					Client.Artefacts.Update(Client.Artefacts.Single(artefact => artefact.Aspects.Contains(aDbDisk)));
//					Log.Info("Drive already exists in DB, would update here");
					Log.InfoVariable("aDbDrive", aDbDrive);
				}
				else
				{
					Artefact artefact = new Artefact(new Uri("drive://" + drive.Partition), drive);
					Client.Artefacts.Create(artefact);
//					Log.Info("Drive created in DB");
					Log.InfoVariable("newDrive", drive);
				}
			}
		}

		/// <summary>
		/// Enumerates the directories.
		/// </summary>
		/// <returns>The directories.</returns>
		/// <param name="uri">URI.</param>
		protected virtual IEnumerable<string> EnumerateDirectories(string uri)
		{
			Log.DebugVariable("uri", uri);
//			if (!uri.IsFile)
//				throw new NotImplementedException("Only URIs with a file schema are currently supported");
			return System.IO.Directory.EnumerateDirectories(uri, "*", SearchOption.TopDirectoryOnly);
		}

		/// <summary>
		/// Enumerates the files.
		/// </summary>
		/// <returns>The files.</returns>
		/// <param name="uri">URI.</param>Required
		protected virtual IEnumerable<string> EnumerateFiles(string uri)
		{
			Log.DebugVariable("uri", uri);
//			if (!uri.IsFile)
//				throw new NotImplementedException("Only URIs with a file schema are currently supported");
			return System.IO.Directory.EnumerateFiles(uri, "*", SearchOption.TopDirectoryOnly);
		}
		#endregion
	}
}

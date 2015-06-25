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
using ServiceStack.Text;
using System.Runtime.Serialization;
using System.ServiceModel.Security.Tokens;
using Artefacts.FileSystem;

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
		new private static IDebugLog Log = new DebugLog<ConsoleLogger>(typeof(Artefact)) { SourceClass = typeof(FileSystemAgent) };
		#endregion

		#region Fields & properties
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

		public IQueryable<Disk> Disks;
		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.FileSystemArtefactCreator"/> class.
		/// </summary>
		/// <param name="repository">Repository.</param>
		public FileSystemAgent(IDataStore client) : base(client)
		{
			if (client == null)
				throw new ArgumentNullException("client");
			BaseUri = new UriBuilder(Uri.UriSchemeFile, "localhost", 0, "/", "?*").Uri;
			Log.Debug(this);
		}

		/// <summary>
		/// Run the specified param.
		/// </summary>
		/// <param name="param">Parameter.</param>
		/// <remarks>Artefacts.CreatorBase implementation</remarks>
		public override void Run(object param)
		{
			MarkStart();
//			Log.DebugVariable("Disk.Disks.Count", Disk.Disks.Count);
//			Queryable<Artefact> testArtefacts = (Queryable<Artefact>)Client.Artefacts.Where((a) => a.TimeCreated > new DateTime(2015, 02, 01));
//			Log.DebugVariable("testArtefacts.Count", testArtefacts.Count);
//			foreach (Artefact artefact in testArtefacts)
//				Log.DebugVariable("artefact", artefact);
//			Queryable<Artefact> diskArtefacts = (Queryable<Artefact>)Client.Artefacts.Where((a) => true);
//			Log.DebugVariable("diskArtefacts.Count", diskArtefacts.Count);
//			foreach (Artefact artefact in diskArtefacts)
//				Log.DebugVariable("artefact", artefact);
//			diskArtefacts = (Queryable<Artefact>)Client.Artefacts.Where((a) => a.AspectTypeNames.Contains(typeof(Disk).FullName));
//			foreach (Artefact artefact in diskArtefacts)
//				Log.DebugVariable("artefact", artefact);
//			Log.DebugVariable("diskArtefacts.Count", diskArtefacts.Count);
//			Disks = Client.Artefacts.Where(
//				(a) => (bool)a.Data.SingleOrDefault(			// a.AspectTypeNames.Contains(typeof(Disk).FullName)).Select((a) => (Disk)a[typeof(Disk)].Instance);
//					(aspect) => aspect.AspectType == typeof(Disk).FullName))
//						.Select((artefact) => artefact.Data.Select((aspect) => (Disk)aspect.Instance));
//			foreach (Artefact artefact in Disks)
//				Log.DebugVariable("artefact", artefact);
//			Log.DebugVariable("Disks.Count", Disks.Count);
//			Disks = Client.Artefacts.SelectMany<Artefact, Aspect>(a => a.Aspects)
//				.Where(aspect => aspect.Type.Equals(typeof(FileSystem.Disk)))	// is FileSystem.Disk)
//				.Select(aspect => aspect.As<Disk>());
//			Disks
//			IEnumerable<Aspect> dAs = Client.Aspects.Where(aspect => aspect.TypeName.Equals("FileSystem.Disk"));

			IEnumerable<Artefact> dAs = Client.Artefacts.Where(artefact => artefact.Aspects.Any(aspect => aspect.TypeName.Equals("Artefacts.FileSystem.Disk")));
			//Name == typeof(FileSystem.Disk).FullName);

//					.Select((aspect) => (Disk)aspect.Instance);
			//.OfType<Disk>();//((aspect) => aspect.Type == typeof(Disk));	//.Get<Disk>((disk) => true).AsQueryable();//Aspects.Where((aspect) => aspect.AspectType == typeof(Disk).FullName).Select((aspect) => (Disk)aspect.Instance);
//			Log.Info("Database Disks: " + Disks.SerializeToString());
			Log.InfoVariable("Database Disks", dAs);
			Log.InfoVariable("Detected Disks", Disk.Disks);
			foreach (Disk disk in Disk.Disks)
			{
				Artefact a = dAs.FirstOrDefault(
					(artefact) => artefact.Aspects.Any(
						aspect => aspect.TypeName.Equals("Artefacts.FileSystem.Disk") && aspect.As<FileSystem.Disk>().Serial == disk.Serial));
				if (a != null)
				{
					Disk aDbDisk// = null;
					= a.Aspects[typeof(Disk)].As<Disk>();
							//.).SingleOrDefault((dbDisk) => dbDisk.Aspects.SingleOrDefault(.As<Disk>().Serial == disk.Serial).As<Disk>();
				
//					Client.Artefacts.Update(Client.Artefacts.Single(artefact => artefact.Aspects.Contains(aDbDisk)));
					Log.Info("Disk already exists in DB, would update here");
					Log.InfoVariable("aDbDisk", aDbDisk);
				}
				else
				{
					Artefact artefact = new Artefact(new Uri("disk://" + disk.Serial), disk);
					Client.Artefacts.Create(artefact);
					Log.Info("Disk created in DB");
					Log.InfoVariable("newDisk", disk);
				}
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


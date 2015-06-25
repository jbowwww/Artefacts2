using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Diagnostics;

namespace Artefacts.FileSystem
{
	/// <summary>
	/// File system entry.
	/// </summary>
	public class Drive
	{
		#region Static members
		/// <summary>
		/// Gets the partition mount paths.
		/// </summary>
		/// <value>The partition mount paths.</value>
		public static IDictionary<string, string> PartitionMountPaths {
			get
			{
				if (_partitionMountPaths == null)
				{
					_partitionMountPaths = new ConcurrentDictionary<string, string>();
					Process getMountProcess = Process.Start(
						new ProcessStartInfo("mount")
						{
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
					});
					getMountProcess.WaitForExit(1600);
					string mountOutput;
					while (!string.IsNullOrEmpty(mountOutput = getMountProcess.StandardOutput.ReadLine()))
					{
						string[] splitOutput = mountOutput.Split(' ');
						if (splitOutput.Length <= 5 || splitOutput[1] != "on" || splitOutput[3] != "type")
							throw new InvalidDataException("Unexpected output data from mount command");
						_partitionMountPaths[splitOutput[2]] = splitOutput[0];
					}
				}
				return _partitionMountPaths;				
			}
		}
		private static IDictionary<string, string> _partitionMountPaths = null;

		/// <summary>
		/// Gets the drives. New 09/05/15 after old version using DriveInfo.GetDrives causing SIGSEGV crashes
		/// </summary>
		/// <returns>The drives.</returns>
		/// <remarks>
		/// Recently (couple days prior to 17/5/15) changed from using mount command to df --total --output
		/// due to crashes/SIGABRT issues. Now seems to work as needed.
		/// </remarks>
		public static IEnumerable<Drive> GetDrives()
		{
			List<Drive> drives = new List<Drive>();
			Process getDfProcess = Process.Start(
				new ProcessStartInfo("df")
				{
				Arguments = " --total --output",
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
				});
			getDfProcess.WaitForExit(600);
			string dfOutput;
			bool firstLine = true;
			while (!string.IsNullOrEmpty(dfOutput = getDfProcess.StandardOutput.ReadLine()))
			{
				if (firstLine)
					firstLine = false;
				else
				{
					string[] splitOutput = dfOutput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					if (splitOutput.Length < 11)
						throw new InvalidDataException("Unexpected output data from df command");
					long size = long.Parse(splitOutput[6]) * 1024;
					long free = size - (long.Parse(splitOutput[7]) * 1024);
					Drive drive = new Drive() {
						Partition = splitOutput[0],
						Format = splitOutput[1],
						Size = size,
						FreeSpace = free,
						AvailableFreeSpace = long.Parse(splitOutput[8]) * 1024,
						Name = splitOutput[10],
						Label = splitOutput[10]
					};
					drives.Add(drive);
				}
			}
			return drives;
		}

		/// <summary>
		/// Gets the drives.
		/// </summary>
		/// <value>The drives.</value>
		public static IEnumerable<Drive> All {
			get { return _all ?? (_all = GetDrives()); } //new List<Drive>(); }
		}
		private static IEnumerable<Drive> _all = null;
		#endregion

		#region Public fields & properties
		/// <summary>
		/// Gets or sets the disk.
		/// </summary>
		public Disk Disk;

		/// <summary>
		/// Gets or sets the disk serial.
		/// </summary>
		public string DiskSerial {
			get { return Disk == null ? (_diskSerial ?? string.Empty) : Disk.Serial; }
			set { _diskSerial = value; }// Disk = Disk.Disks.SingleOrDefault(disk => disk.Serial == value); }
		}
		public string _diskSerial;

		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		public string Name;	// { get; set; }

		/// <summary>
		/// Gets or sets the partition.
		/// </summary>
		public string Partition;	// { get; set; }

		/// <summary>
		/// Gets or sets the label.
		/// </summary>
		public string Label;	// { get; set; }

		/// <summary>
		/// Gets or sets the format.
		/// </summary>
		public string Format;	// { get; set; }

		/// <summary>
		/// Gets or sets the type.
		/// </summary>
		public DriveType Type;	// { get; set; }

		/// <summary>
		/// Gets or sets the size.
		/// </summary>
		public long Size;	// { get; set; }

		/// <summary>
		/// Gets or sets the free space.
		/// </summary>
		public long FreeSpace;	// { get; set; }

		/// <summary>
		/// Gets or sets the available free space.
		/// </summary>
		public long AvailableFreeSpace;	// { get; set; }

		/// <summary>
		/// Gets or sets the drive info.
		/// </summary>
		public DriveInfo DriveInfo {
			get { return _driveInfo; }
			set
			{
				_driveInfo = value;
				Name = _driveInfo.Name;
				Label = _driveInfo.VolumeLabel;
				Format = _driveInfo.DriveFormat;
				Type = _driveInfo.DriveType;
				Size = _driveInfo.TotalSize;
				FreeSpace = _driveInfo.TotalFreeSpace;
				AvailableFreeSpace = _driveInfo.AvailableFreeSpace;
//				if (Drive.PartitionMountPaths != null)
//					Partition = PartitionMountPaths.ContainsKey(Label) ? PartitionMountPaths[Label] : string.Empty;
//				Disk = Disk.Disks.FirstOrDefault((disk) => Name.ToLower().StartsWith(disk.DeviceName.ToLower()));
			}
   		}
		private DriveInfo _driveInfo;
		#endregion

		#region Methods
		#region Construction
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.Drive"/> class.
		/// </summary>
		public Drive()
		{

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.Drive"/> class.
		/// </summary>
		/// <param name="driveName">Drive name.</param>
		public Drive(string driveName)
		{
			DriveInfo = new DriveInfo(driveName);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.Drive"/> class.
		/// </summary>
		/// <param name="driveInfo">Drive info.</param>
		protected Drive(DriveInfo driveInfo)
		{
			DriveInfo = driveInfo;
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
			if (!typeof(Drive).IsAssignableFrom(obj.GetType()))
				return false;
			Drive drive = (Drive)obj;
			return drive.Partition == Partition;
//				drive.Label == Label;
		}

		/// <summary>
		/// Serves as a hash function for a <see cref="Artefacts.FileSystem.FileSystemEntry"/> object.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode()
		{
			return Partition.GetHashCode();
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="Artefacts.FileSystem.Disk"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Artefacts.FileSystem.Disk"/>.</returns>
		public override string ToString()
		{
			return this.ToString();
		}
		#endregion
		#endregion
	}
}
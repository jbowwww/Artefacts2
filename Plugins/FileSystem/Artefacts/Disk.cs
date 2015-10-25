using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Artefacts.FileSystem
{
	/// <summary>
	/// Disk representation
	/// </summary>
	public class Disk
	{
		#region Static fields
		/// <summary>
		/// Gets the disks.
		/// </summary>
		public static List<Disk> Disks {
			get { return _disks != null ? _disks : _disks = Disk.GetAll(Host.Current); }
		}
		private static List<Disk> _disks;

		/// <summary>
		/// Refreshs the disks.
		/// </summary>
		public static void RefreshDisks()
		{
			_disks = null;
		}

		/// <summary>
		/// Gets the disks.
		/// </summary>
		/// <returns>The disks.</returns>
		/// <param name="host">Host.</param>
		private static List<Disk> GetAll(Host host)
		{
			List<Disk> disks = new List<Disk>();
			Process lsblkProcess = Process.Start(
				new ProcessStartInfo("lsblk")
				{
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					Arguments = "-lb"
				});
			lsblkProcess.WaitForExit(1600);
			string lsblkOutput;
			while (!string.IsNullOrEmpty(lsblkOutput = lsblkProcess.StandardOutput.ReadLine()))
			{
				string[] tokens = lsblkOutput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if (lsblkOutput.Contains("disk"))
				{
					Disk disk = new Disk()
					{
						Host = null,//host,
						Name = tokens[0],
						Size = long.Parse(tokens[3])
					};
					disk.GetDetails();
					disks.Add(disk);
				}
			}
			return disks;
		}
		#endregion

		#region Public fields & properties
		/// <summary>
		/// Disk serial string
		/// </summary>
		public string Serial;

		/// <summary>
		/// <see cref="Host"/> the <see cref="Disk"/> was last seen on
		/// </summary>
		public Host Host;

		/// <summary>
		/// Name the <see cref="Disk"/> was referred to as on the host it was last seen on
		/// </summary>
		public string Name;

		/// <summary>
		/// The size.
		/// </summary>
		public long Size;

		/// <summary>
		/// The model.
		/// </summary>
		public string Model;
		#endregion

		#region Methods
		#region Construction
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.FileSystem.Disk"/> class.
		/// </summary>
		public Disk()
		{
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
			if (!typeof(Disk).IsAssignableFrom(obj.GetType()))
				return false;
			Disk d = (Disk)obj;
//			return Host == Host.Current && string.Equals(Name, d.Name);
//			return string.Equals(Name, d.Name);
			return string.Equals(Serial, d.Serial);
		}

		/// <summary>
		/// Serves as a hash function for a <see cref="Artefacts.FileSystem.FileSystemEntry"/> object.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode()
		{
			return Serial.GetHashCode();
		}
		#endregion

		/// <summary>
		/// Gets disk serial and model string
		/// </summary>
		/// <param name="name">Disk device name</param>
		private void GetDetails()
		{
			Process udevadmProcess = Process.Start(
				new ProcessStartInfo("udevadm")
				{
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					UseShellExecute = false,
					Arguments = string.Concat("info --name=", Name)
				});
			udevadmProcess.WaitForExit(444);
			string udevadmOutput;
			while (!string.IsNullOrEmpty(udevadmOutput = udevadmProcess.StandardOutput.ReadLine())
				&&	(string.IsNullOrEmpty(Serial) || string.IsNullOrEmpty(Model)))
			{
				if (udevadmOutput.StartsWith("E: ID_SERIAL_SHORT="))
					Serial = udevadmOutput.Substring(19);
				else if (udevadmOutput.StartsWith("E: ID_MODEL="))
					Model = udevadmOutput.Substring(12);
			}
		}
		#endregion
	}
}


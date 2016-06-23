using System;

namespace Artefacts.FileSystem
{
	public static class Size_Ext
	{
		public static string FormatSize(this long size)
		{
			return Size.Format(size);
		}
	}
	public struct Size
	{
		public static Size Bytes(long size) { return new Size(size); }
		public static Size KiloBytes(long kB) { return new Size(kB * Size.KiloByte); }
		public static Size MegaBytes(long mB) { return new Size(mB * Size.MegaByte); }
		public static Size GigaBytes(long gB) { return new Size(gB * Size.GigaByte); }
		public static Size TeraBytes(long tB) { return new Size(tB * Size.TeraByte); }
		public static Size PetaBytes(long pB) { return new Size(pB * Size.PetaByte); }

		public static Size Byte = 1;
		public static Size KiloByte = 1024;
		public static Size MegaByte = 1024 * 1024;
		public static Size GigaByte = 1024 * 1024 * 1024;
		public static Size TeraByte = 1024L * 1024 * 1024 * 1024;
		public static Size PetaByte = 1024L * 1024 * 1024 * 1024 * 1024;

		// ..?
		private long _instanceSize;

		public long NumBytes { get { return _instanceSize; } }
		public double NumKiloBytes { get { return (double)_instanceSize / KiloByte; } }
		public double NumMegaBytes { get { return (double)_instanceSize / MegaByte; } }
		public double NumGigaBytes { get { return (double)_instanceSize / GigaByte; } }
		public double NumTeraBytes { get { return (double)_instanceSize / TeraByte; } }
		public double NumPetaBytes { get { return (double)_instanceSize / PetaByte; } }

		internal Size(long size)
		{
			_instanceSize = size;
		}
		internal Size(long size, Size unitsMultiplier)
		{
			_instanceSize = size * unitsMultiplier;
		}
		public string Format()
		{
			return Size.Format((long)this);
		}
		public static string Format(Size size)
		{
			return Size.Format((long)size);
		}
//		public static string Format(this long size)
//		{
//			return Size.Format(size);
//		}
		public static string Format(long size)
		{
			string[] units = { "B", "KB", "MB", "GB", "TB" };
			double s = (double)size;
			int unitIndex = 0;
			while (s > 1024 && unitIndex < units.Length)
			{
				unitIndex++;
				s /= 1024;
			}
			return string.Concat(s.ToString("N2"), units[unitIndex]);
		}
		public static implicit operator long(Size size)
		{
			return size._instanceSize;
		}
		public static implicit operator Size(long size)
		{
			return new Size(size);
		}
	};
}


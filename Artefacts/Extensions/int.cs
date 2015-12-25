using System;
namespace Artefacts.Extensions
{
	public static class Int_Ext
	{
		public static string ToHex(this ulong value)
		{
			return string.Format("{0:x}", value);
		}
		
		public static string ToHex(this long value)
		{
			return string.Format("{0:x}", value);
		}
	}
}


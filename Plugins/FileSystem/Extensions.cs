using System;
using System.Linq;
using System.Collections.Generic;

namespace Artefacts.FileSystem
{
	/// <summary>
	/// Extensions.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Froms the path.
		/// </summary>
		/// <returns>The path.</returns>
		/// <param name="fsEntries">Fs entries.</param>
		/// <param name="rootPath">Root path.</param>
		public static FileSystemEntry FromPath(this IEnumerable<FileSystemEntry> fsEntries, string rootPath)
		{
//			if (fsEntries == null)
//				throw new NullReferenceException("drives");
			return (from fse in fsEntries
				where
					fse.Path.Length < rootPath.TrimEnd('/', '\\').Length
					&& rootPath.StartsWith(fse.Path)
				orderby fse.Path.Length descending
				select fse).FirstOrDefault();
		}

		/// <summary>
		/// Froms the path.
		/// </summary>
		/// <returns>The path.</returns>
		/// <param name="drives">Drives.</param>
		/// <param name="rootPath">Root path.</param>
		public static Drive FromPath(this IEnumerable<Drive> drives, string rootPath)
		{
//			if (drives == null)
//				throw new NullReferenceException("drives");
			return (from d in drives
				where rootPath.StartsWith(d.Label)		//					.Where((d) => rootPath.StartsWith(d.Label))
				orderby d.Label.Length descending		//					.OrderByDescending((d) => d.Label.Length)
				select d).FirstOrDefault();
		}
	}
}


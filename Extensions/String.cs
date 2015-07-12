using System;
using System.Text;

namespace Artefacts
{
	/// <summary>
	/// String_ ext.
	/// </summary>
	public static class String_Ext
	{
		/// <summary>
		/// Repeat the specified text and count.
		/// </summary>
		/// <param name="text">Text.</param>
		/// <param name="count">Count.</param>
		public static string Repeat(this string text, int count)
		{
			if (text == null)
				throw new ArgumentNullException("text");
			else if (text == string.Empty)
				return string.Empty;
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", count, "Less than zero");
			StringBuilder sb = new StringBuilder(text.Length * count);
			for (int i = count; i > 0; i--)
				sb.Append(text);
			return sb.ToString();
		}

		/// <summary>
		/// Trims the start.
		/// </summary>
		/// <returns>The start.</returns>
		/// <param name="text">Text.</param>
		/// <param name="trim">Trim.</param>
		/// <param name="comparison">Comparison.</param>
		public static string TrimStart(this string text, string trim, StringComparison comparison = StringComparison.CurrentCulture)
		{
			return string.IsNullOrEmpty(trim) ? text : text.StartsWith(trim, comparison) ? text.Substring(trim.Length) : text;
		}

		/// <summary>
		/// Trims the start.
		/// </summary>
		/// <returns>The start.</returns>
		/// <param name="text">Text.</param>
		/// <param name="trim">Trim.</param>
		/// <param name="comparison">Comparison.</param>
		public static string TrimEnd(this string text, string trim, StringComparison comparison = StringComparison.CurrentCulture)
		{
			return string.IsNullOrEmpty(trim) ? text : text.EndsWith(trim, comparison) ? text.Substring(0, text.Length - trim.Length) : text;
		}

		/// <summary>
		/// Trim the specified text, trim and comparison.
		/// </summary>
		/// <param name="text">Text.</param>
		/// <param name="trim">Trim.</param>
		/// <param name="comparison">Comparison.</param>
		public static string Trim(this string text, string trim, StringComparison comparison = StringComparison.CurrentCulture)
		{
			return text.TrimStart(trim, comparison).TrimEnd(trim, comparison);
		}

		/// <summary>
		/// Compares the ignore case.
		/// </summary>
		/// <returns>The ignore case.</returns>
		/// <param name="text">Text.</param>
		/// <param name="compare">Compare.</param>
		public static int CompareIgnoreCase(this string text, string compare)
		{
			return text.ToUpper().CompareTo(compare.ToUpper());
		}
	}
}


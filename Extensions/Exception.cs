using System;
using ServiceStack;
using System.Text;
using System.Runtime.InteropServices;

namespace Artefacts
{
	/// <summary>
	/// Exception_ ext.
	/// </summary>
	public static class ExceptionExtensions
	{
		/// <summary>
		/// Format the specified exception.
		/// </summary>
		/// <param name="exception">Exception.</param>
		public static string Format(this Exception ex, string indentString = "   ", int indentLevel = 0)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine(ex.GetType().FullName);
			if (ex is WebServiceException)
			{
				WebServiceException we = (WebServiceException)ex;
				sb.AppendFormat(
					"{0}Status: {1}: {2}\n{0}Error: {3}: {4}\n{0}Source: {5}\n{0}TargetSite: {9}\n{0}Data: {10}\n" +
					"{0}{12}\n{0}InnerException: {11}\n{0}Total Errors: {6}",	//\n{0}Response Body: {7}",
					indentString.Repeat(indentLevel), we.StatusCode, we.StatusDescription, we.ErrorCode, we.ErrorMessage,
					we.Source, we.ResponseStatus.Errors == null ? 0 : we.ResponseStatus.Errors.Count, we.ResponseBody,
					we.StackTrace.Trim().Replace("\n", string.Concat("\n", indentString.Repeat(indentLevel + 1))),
					we.TargetSite.ReflectedType.Name + ": " + we.TargetSite.Name, ex.Data.Count,
					we.InnerException == null ? "(null)" : we.InnerException.Format(indentString, indentLevel + 1),
					we.ServerStackTrace == null ? 
							(ex.StackTrace == null ? "[Server]StackTrace: (null)" : string.Concat("StackTrace: \n", indentString.Repeat(indentLevel + 1),
							ex.StackTrace.Trim().Replace("\n", string.Concat("\n", indentString.Repeat(indentLevel)))))
						 : string.Concat("ServerStackTrace: \n", indentString.Repeat(indentLevel + 1),
						we.ServerStackTrace.Trim().Replace("\n", string.Concat("\n", indentString.Repeat(indentLevel))),
							we.StackTrace != null ?
								string.Concat("StackTrace: \n", indentString.Repeat(indentLevel + 1),
								ex.StackTrace.Trim().Replace("\n", string.Concat("\n", indentString.Repeat(indentLevel)))) : ""));
				if (we.ResponseStatus.Errors != null && we.ResponseStatus.Errors.Count > 0)
				{
					foreach (ResponseError error in we.ResponseStatus.Errors)
						sb.Append("Error[]: " + error.FieldName + ": " + error.Message);
					sb.AppendFormat("{0}StackTrace:{0}{1}{2}",
						string.Concat("\n", indentString.Repeat(indentLevel)),
						indentString.Repeat(indentLevel + 1),
						we.ResponseStatus.StackTrace);
				}
			}
			else
			{
				sb.AppendFormat("{0}Message: {1}\n{0}Source: {2}\n{0}TargetSite: {5}\n{0}Data: {3}\n{0}StackTrace: {4}\n{0}InnerException: {6}",
					indentString.Repeat(indentLevel), ex.Message, ex.Source, ex.Data.Count,	//FormatString(indentLevel, indentString),
					ex.StackTrace == null ? "(null)" : string.Concat("\n", indentString.Repeat(indentLevel + 1),
						ex.StackTrace.Trim().Replace("\n", string.Concat("\n", indentString.Repeat(indentLevel)))),
					ex.TargetSite.ReflectedType.Name + ": " + ex.TargetSite.Name,
					ex.InnerException == null ? "(null)" : ex.InnerException.Format(indentString, indentLevel + 1));
			}
			return sb.ToString();
		}
	}
}


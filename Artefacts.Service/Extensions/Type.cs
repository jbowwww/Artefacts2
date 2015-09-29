using System;
using System.Collections.Generic;
using System.Linq;

namespace Artefacts
{
	public static class Type_Extensions
	{
		public static bool IsPredicate(this Type type)
		{
			return typeof(Delegate).IsAssignableFrom(type);
		}
		
		public static bool IsDelegate(this Type type)
		{
			return typeof(Delegate).IsAssignableFrom(type);
		}
		
		public static T GetCustomAttribute<T>(this Type type, bool inherit = true)
		{
			return (T)new List<object>(type.GetCustomAttributes(typeof(T), inherit)).FirstOrDefault();
		}
	}
}


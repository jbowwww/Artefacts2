using System;
using System.Collections.Generic;
using System.Linq;

namespace Artefacts.Extensions
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
		
		public static bool IsLargeInteger(this Type T)
		{
			return typeof(ulong).IsAssignableFrom(T);
			// c.Type.IsPrimitive && c.Type.IsValueType && 
		}
	}
}


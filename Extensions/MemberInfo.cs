using System;
using System.Reflection;

namespace Artefacts
{
	public static class MemberInfo_Ext
	{
		/// <summary>
		/// Checks if the type of <paramref name="instance"/>  has a member equivalent to <paramref name="member"/>
		/// </summary>
		/// <returns><c>true</c>, if member in was equivalented, <c>false</c> otherwise.</returns>
		/// <param name="member">Member.</param>
		/// <param name="instance">Instance.</param>
		public static bool EquivalentMemberIn(this MemberInfo member, object instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
			return EquivalentMemberIn(member, instance.GetType());
		}

		/// <summary>
		/// Checks if a type has a member equivalent to <paramref name="member"/>
		/// </summary>
		/// <returns><c>true</c>, if member in was equivalented, <c>false</c> otherwise.</returns>
		/// <param name="member">Member.</param>
		/// <param name="type">Type.</param>
		public static bool EquivalentMemberIn(this MemberInfo member, Type type)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			if (type == null)
				throw new ArgumentNullException("type");
			MemberInfo[] member2s = type.GetMember(member.Name);
			if (member2s == null || member2s.Length == 0)
				return false;
			foreach (MemberInfo member2 in member2s)
				if (member.MemberType.Equals(member2.MemberType)
				    && member.DeclaringType.Equals(member2.DeclaringType)
				    && member.Name.Equals(member2.Name))
					return true;	// TODO: Should really check/cast for/to Methods, Properties, Fields & check return/data type
			return false;
		}

		/// <summary>
		/// Gets the type of the member return.
		/// </summary>
		/// <returns>The member return type.</returns>
		/// <param name="member">Member.</param>
		public static Type GetMemberReturnType(this MemberInfo member)
		{
			return member.MemberType == MemberTypes.Field ? ((FieldInfo)member).FieldType
				: member.MemberType == MemberTypes.Property ? ((PropertyInfo)member).PropertyType
					: member.MemberType == MemberTypes.Method ? ((MethodInfo)member).ReturnType : null;
		}


		public static object GetPropertyOrField(this MemberInfo member, object instance)
		{
			if (member.MemberType == MemberTypes.Field)
				return ((FieldInfo)member).GetValue(instance);
			if (member.MemberType == MemberTypes.Property)
				return ((PropertyInfo)member).GetValue(instance);
			throw new ArgumentOutOfRangeException("member", "MemberType == " + member.MemberType.ToString());
		}
		
		public static object GetValue(this PropertyInfo property, object instance)
		{
			return property.GetValue(instance, new object[] { });
		}
	}
}


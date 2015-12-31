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
				return ((PropertyInfo)member).GetValue(instance, new object[] { });
			throw new ArgumentOutOfRangeException("member", "MemberType == " + member.MemberType.ToString());
		}
		
		public static object GetValue(this MemberInfo member, object instance)
		{
			return
				member.MemberType == MemberTypes.Property	? ((PropertyInfo)member).GetValue(instance, new object[] { })
			:	member.MemberType == MemberTypes.Field 		? ((FieldInfo)member).GetValue(instance) : null;
		}
		
		public static void SetValue(this MemberInfo member, object instance, object value)
		{
			Type memberType = member.GetMemberReturnType();
			Type valueType = value == null ? typeof(object) : value.GetType();
			if (member.MemberType == MemberTypes.Property)
				((PropertyInfo)member).SetValue(instance, memberType.IsAssignableFrom(valueType) ? value :
					Convert.ChangeType(value, ((PropertyInfo)member).PropertyType));
			else if (member.MemberType == MemberTypes.Field)
				((FieldInfo)member).SetValue(instance, memberType.IsAssignableFrom(valueType) ? value :
					Convert.ChangeType(value, ((PropertyInfo)member).PropertyType));
			else
				throw new MemberAccessException(string.Format("Wrong member type ({0}) for member \"{1}\"", member.MemberType, member.Name));
		}
		
		public static bool IsPublic(this MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException("member");
			Type memberType = member.GetType();
			if (typeof(FieldInfo).IsAssignableFrom(memberType))
				return ((FieldInfo)member).IsPublic;
			if (typeof(PropertyInfo).IsAssignableFrom(memberType))
			{
				MethodInfo getMethod = ((PropertyInfo)member).GetGetMethod();
				MethodInfo setMethod = ((PropertyInfo)member).GetSetMethod();
				return
					getMethod != null && getMethod.IsPublic &&
					setMethod != null && setMethod.IsPublic;
			}
			if (typeof(MethodInfo).IsAssignableFrom(memberType))
				return ((MethodInfo)member).IsPublic;
			throw new ArgumentOutOfRangeException("member", member, "Not a supported member type (type is \"" + memberType.FullName + "\")");
		}
	}
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Reflection;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;

namespace Artefacts
{
	/// <summary>
	/// Extension methods for <see cref="System.Object"/>
	/// </summary>
	public static class Object_Ext
	{
		
		public static Type GetType(this object o)
		{
			return o != null ? o.GetType() : typeof(object);
		}
		
		
		///
		internal class FormatStringContext
		{
			public StreamingContext StreamingContext;
			public object Root;
			public object Current;
			public Hashtable FormattedObjects = new Hashtable();
			public int MaxDepth = 2;
			public int OutputWidth = Console.WindowWidth;
			public int IndentLevel = 0;
			public string IndentString = "   ";
			public string Indent { get { return IndentString.Repeat(IndentLevel); } }
			public string Indent2 { get { return string.Concat(Indent, IndentString); } }
			public BindingFlags GetMembersBindingFlags = BindingFlags.Instance | BindingFlags.DeclaredOnly |
				BindingFlags.Public /*| BindingFlags.NonPublic*/ | BindingFlags.GetField | BindingFlags.GetProperty;
			public Func<MemberInfo, bool> GetMembersWhereClause = (mi) =>
					((	mi.MemberType == MemberTypes.Field
					 && !((FieldInfo)mi).Name.EndsWith(">k__BackingField"))
				 || (mi.MemberType == MemberTypes.Property
				 && mi.Name.CompareIgnoreCase("item") != 0));

			public FormatStringContext(object obj, StreamingContextStates streamingContextState = StreamingContextStates.Persistence)
			{
				Initialise(obj, new StreamingContext(streamingContextState, this));
			}

			public FormatStringContext(object obj, StreamingContext streamingContext)
			{
				Initialise(obj, streamingContext);
			}

			public FormatStringContext(object obj)
			{
				Initialise(obj, new StreamingContext(StreamingContextStates.Persistence, this));
			}

			private void Initialise(object obj, StreamingContext streamingContext)
			{
				Root = obj;
				Current = obj;
				StreamingContext = streamingContext;
			}

			public FormatStringContext IncrementIndent()
			{
				IndentLevel++;
				return this;
			}

			public FormatStringContext DecrementIndent()
			{
				IndentLevel--;
				return this;
			}

			public FormatStringContext PushCurrentObject()
			{
				if (FormattedObjects.ContainsKey(Current))
					throw new InvalidOperationException("FormatStringContext.FormattedObjects already contains the current object (" + Current.ToString() + ")");
				FormattedObjects.Add(Current, Current);
				return this;
			}

			public static implicit operator StreamingContext(FormatStringContext context)
			{
				return context.StreamingContext;
			}
		}

		
		/// <summary>
		/// Formats the <see cref="System.Object"/> to a <see cref="System.String"/>
		/// </summary>
		/// <returns>The formatted <see cref="System.String"/></returns>
		/// <param name="o">The <see cref="System.Object"/> to format</param>
		public static string FormatString(this object obj, int maxDepth = 2, string indentString = "   ", int indentLevel = 0)
		{
			return FormatString(obj, new FormatStringContext(obj) {
				IndentLevel = indentLevel,
				MaxDepth = maxDepth,
				IndentString = indentString
			});
		}

		/// <summary>
		/// Formats the string.
		/// </summary>
		/// <returns>The string.</returns>
		/// <param name="obj">Object.</param>
		/// <param name="context">Context.</param>
		/// <remarks>
		/// TODO: Optimise/refactor this method to reduce code repetition
		/// TODO: ^ Partly done, is recursive.
		/// TODO: Still need to fine tune display of nested/enumerable structures
		/// </remarks>
		internal static string FormatString(this object obj, FormatStringContext context)
		{
			if (obj == null)
				return "(null)";
//				throw new ArgumentNullException("obj");
			Type type = obj.GetType();
			if (type == typeof(string))
			{
				int space = context.OutputWidth - context.IndentString.Length - 8;
				return ((string)obj).Length <= space ? obj.ToString()
					: string.Concat("\"", ((string)obj).Substring(0, space), " ...", "\"");
			}
			Type[] primitiveExtraTypes = new Type[] { /*typeof(string)*/ typeof(DateTime), typeof(TimeSpan) };
			if (type.IsPrimitive || type.IsEnum || primitiveExtraTypes.Contains(type) )
				return obj.ToString();
			if (context == null)
				context = new FormatStringContext(obj);
			else if (context.IndentLevel == context.MaxDepth)
				return obj.ToString() +	" ...";
			if (context.FormattedObjects.ContainsKey(obj))
				return obj.ToString() + " ... *";
			context.FormattedObjects.Add(obj, obj);

			if (context.IndentString == null)
				throw new ArgumentNullException("indent");
			context.Current = obj;

			StringBuilder sb = new StringBuilder();

			if (type.IsSubclassOf(typeof(DynamicObject)))
			{
				DynamicObject objAsDynamic = (DynamicObject)obj;
				context.IncrementIndent();
				sb.Append(string.Concat("[", type.FullName, " as DynamicObject]"));
				foreach (string member in objAsDynamic.GetDynamicMemberNames())
				{
					object memberValue =
						objAsDynamic.GetMetaObject(Expression.PropertyOrField(Expression.Constant(objAsDynamic), member)).Value;
					
					sb.AppendFormat("\n{0}{1}: {2}", context.Indent2, member, FormatString(memberValue, context));
				}
			}
			else if (type.GetInterface("ISerializable") != null && (type.GetInterface("IEnumerable") == null || type.Namespace.StartsWith("Artefacts")))
			{
				SerializationInfo info = new SerializationInfo(type, new FormatterConverter());
				ISerializable objAsSerializable = (ISerializable)obj;
				objAsSerializable.GetObjectData(info, context);
				context.IncrementIndent();
				sb.Append(string.Concat(/*info.MemberCount > 0 ? string.Concat("\n", context.Indent, "[")
					:*/ "[", type.FullName, " as ISerializable].GetObjectData()"));
				foreach (SerializationEntry entry in info)
					sb.AppendFormat("\n{0}{1}: {2}",// ({3})
					                context.Indent2, entry.Name,
					                FormatString(entry.Value, context));
//					                , entry.ObjectType == null ? "" : entry.ObjectType.FullName);
				context.DecrementIndent();
			}
			else
			{
				if (type.Namespace == null || !type.Namespace.StartsWith("System"))
				{
					bool hasSerializableAttribute = type.GetCustomAttribute<SerializableAttribute>(false) != null;

					MemberInfo[] mis = hasSerializableAttribute ?
						FormatterServices.GetSerializableMembers(type, new StreamingContext(StreamingContextStates.Remoting)).ToArray()
					:	type.GetMembers(context.GetMembersBindingFlags).Where(context.GetMembersWhereClause).ToArray();
					sb.Append(string.Concat(mis.Length > 0 ? string.Concat("\n", context.Indent, "[")
						: "[", type.FullName, "]", hasSerializableAttribute ? " marked with [Serializable]" : ""));
					if (mis.Length > 0)
					{
						object[] values = new object[mis.Length];
						for (int i = 0; i < mis.Length; i++)
						{
							try {
								values[i] = type.InvokeMember(mis[i].Name, context.GetMembersBindingFlags, null, obj, new object[] { });
							}
							catch (Exception ex)
							{
								values[i] = string.Concat(ex.GetType().FullName, ": ", ex.Message);
							}
						}
							
//						FormatterServices.GetObjectData(obj, mis.Where((mi) => mi.MemberType == MemberTypes.Field));
						context.IncrementIndent();
						for (int i = 0; i < mis.Length; i++)
						{
							if (!(mis[i].MemberType == MemberTypes.Field &&	((FieldInfo)mis[i]).IsSpecialName))
								sb.AppendFormat("\n{0}{1}: {2}",	// ({3})
					                context.Indent2, mis[i].Name,
					                FormatString(values[i], context));		//, mis[i].ReflectedType == null ? "" : mis[i].ReflectedType.FullName);
						}
						context.DecrementIndent();
					}
				}

				if (type.GetInterface("IDictionary") != null)
				{
					if (type.GetInterface("IDictionary`2") != null)
						type = type.GetInterface("IDictionary`2");
					sb.Append(string.Concat("\n[", type.FullName, "]"));
					context.IncrementIndent();
					if (((IDictionary)obj).Count == 0)
						sb.AppendFormat("\n{0}Empty!", context.Indent);
					foreach (DictionaryEntry de in ((IDictionary)obj))
						sb.AppendFormat("\n{0}{1}: {2}",
						                context.Indent2, de.Key.ToString(),
						                FormatString(de.Value, context));
					context.DecrementIndent();
				}
				else if (type.GetInterface("IEnumerable") != null)
				{
					if (type.GetInterface("IEnumerable`1") != null)
						type = type.GetInterface("IEnumerable`1");
					sb.Append(string.Concat("\n[", type.FullName, "]"));
					context.IncrementIndent();
					int items = 0;
					foreach (object entry in ((IEnumerable)obj))
						sb.AppendFormat("\n{0}{1}", context.Indent2, FormatString(entry, context), items++);
					if (items == 0)
						sb.AppendFormat("\n{0}Empty!", context.Indent);
					context.DecrementIndent();
				}
			}

			return sb.ToString();
		}
		
		
//		public override static string ToString(this object o)
//		{
//			if (o != null && o.GetType().IsArray)
//				return o.ToString().Replace("[]", string.Format("[{0}]", ((Array)o).Length));// string.Format("System.Byte[{0}]", ((IEnumerable)o). ((Byte[])o).Length);
//			return o.ToString();
//		}
	}
}


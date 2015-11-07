using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ServiceStack;
using ServiceStack.Text;
using System.Text;
using Artefacts.Service;
using System.Diagnostics;
using MongoDB.Bson.Serialization;

namespace Artefacts
{
	/// <summary>
	/// Artefact.
	/// </summary>
	/// <remarks>
	/// Two potential approaches to serialization I see at the moment:
	/// 	- Use streams (SS DTO with a Stream member?) to just bang the BsonDocument through
	/// 		- Avoids handling full serialization twice - client side (ServiceStack) and server side (into MongoDB)
	/// 		- No flexibility, depends on MongoDB reference
	/// 		- Not really how it should be done if service is REST
	/// 			- JSON more appopriate - caUseBclJsonSerializersn probably convert relatively cheaply from BsonDocument to JSON
	/// 	- Iterate on elements in the document and add them to SerializationInfo in GetObjectData
	/// 		- ServiceStack can then serialize as it wishes/ how its configured / which client is being
	/// 		  used (JSON, JSV, ProtoBuf, ...)
	/// 		- Seems like double handling of the fields
	/// Try all of the above, compare code readability / format suitability/readability / performance
	/// </remarks>
	[Route("/artefacts/", "POST,PUT")]
	public class Artefact// : DynamicObject	//, IConvertibleToBsonDocument	//, IDictionary<string, object>
	{
		#region Private fields
		private BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty;
		private DataDictionary _artefactData;
		private DataDictionary _persistedData;
		private string _uri = null;
		private ArtefactsClient _client = null;
		#endregion
		
		#region Fields & Properties
		/// <summary>
		/// Gets or sets the artefact's identifier.
		/// </summary>
		[BsonId]
		public string Id {
			get;// { return Data.ContainsKey("_id") ? ObjectId.Parse((string)Data["_id"]) : ObjectId.Parse((string)PersistedData["_id"]); }
			set;// { Data["_id"] = value.ToString(); }
		}

		/// <summary>
		/// Gets or sets the artefact's URI
		/// </summary>
		[BsonIgnore]
		public string Uri {
			get { return _uri ?? (_uri = PathUtils.CombinePaths("/", Collection, Id)); }
		}
		
		/// <summary>
		/// Gets or sets the server-side collection name
		/// </summary>
		[BsonIgnore]
		public string Collection {
			get;// { return (string)this["_collection"]; }
			set;// { this["_collection"] = value; }
		}

		/// <summary>
		/// Gets the <see cref="ArtefactState"/> of this artefact
		/// </summary>
		[BsonIgnore]
		public ArtefactState State {
			get;
			set;
		}
		
		/// <summary>
		/// Gets or sets the time created.
		/// </summary>
		[BsonRequired]
		public DateTime TimeCreated {
			get;// { return (DateTime)this["_timeCreated"]; }
			set;// { this["_timeCreated"] = value; }
		}

		/// <summary>
		/// Gets or sets the time checked.
		/// </summary>
		[BsonRequired]
		public DateTime TimeChecked {
			get;// { return (DateTime)this["_timeChecked"]; }
			set;// { this["_timeChecked"] = value; }
		}

		/// <summary>
		/// Gets or sets the time modified.
		/// </summary>
		[BsonRequired]
		public DateTime TimeModified {
			get;// { return (DateTime)this["_timeModified"]; }
			set;// { this["_timeModified"] = value; }
		}

		/// <summary>
		/// Timestamp when last this <see cref="Artefact"/> was saved/sent to server
		/// </summary>
		[BsonRequired]
		public DateTime TimeSaved {
			get;// { return (DateTime)this["_timeSaved"]; }
			set;// { this["_timeSaved"] = value; }
		}
		
		/// <summary>
		/// Gets or sets the artefact data.
		/// </summary>
		[BsonExtraElements]
		[DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
		public DataDictionary Data {
			get { return _artefactData ?? (_artefactData = new DataDictionary()); }
			set { _artefactData = new DataDictionary(value); }
		}
		
		/// <summary>
		/// Get or set a data member with the given name
		/// </summary>
		/// <param name="name">Name.</param>
		public object this[string name] {
			get
			{
				object result;
				if (!(Data.TryGetValue(name, out result)))
				    throw new ArgumentOutOfRangeException("name", name, "Key not found in Artefact.Data");
				return result;
			}
			set
			{
				Data[name] = value;
			}
		}
		#endregion

		#region Construction / Initialisation
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		internal Artefact()
		{
			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
			TimeSaved = DateTime.MinValue;
			Id = ObjectId.GenerateNewId(TimeCreated).ToString();
			State = ArtefactState.Unknown;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		/// <param name="instance">Instance.</param>
		public Artefact(object instance, ArtefactsClient client = null) : this()
		{
			State = ArtefactState.Created;
			_client = client;
			if (instance == null)
				throw new ArgumentNullException("value");
			if (!instance.GetType().IsClass)
				throw new ArgumentOutOfRangeException("value", "Not a class type");
			if (instance != null)
				SetInstance(instance);
		}
		#endregion

		#region Data handling
		/// <summary>
		/// Sets the instance.
		/// </summary>
		/// <returns>The instance.</returns>
		/// <param name="instance">Instance.</param>
		/// <remarks>
		/// Just made private and only called from c'tor(instance). Therefore should set State = created
		/// and it should remain in that state until pushed to repo for first time ??
		/// ie Artefacts are immutable? answer = NO. Just can't keep setting multiple different instances.
		/// </remarks>
		public int SetInstance(object instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
			Type instanceType = instance.GetType();
			Collection = instanceType.Name;
			IEnumerable<MemberInfo> members = instanceType.GetMembers(_bindingFlags)
				.Where(mi => 
					mi.MemberType == MemberTypes.Field ||
						((mi.MemberType == MemberTypes.Property) &&
				 		(((PropertyInfo)mi).GetSetMethod() != null)));
			foreach (MemberInfo member in members)
			{
				object value = member.GetPropertyOrField(instance);
//				Type valueType = value != null ? value.GetType() : typeof(object);
				//				if (value != null && valueType.IsClass()
				//				 &&	valueType != typeof(string)
				//				 &&	valueType != typeof(DateTime)
				//				 &&	valueType != typeof(TimeSpan))
				//					Data[member.Name] = new Artefact(value);
				//				else
				Data[member.Name] = value;
			}
			return Data.Count;
		}
		
		/// <summary>
		/// As this instance.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T As<T>() //where T : new()
		{
			T instance = Activator.CreateInstance<T>(); //	new T();
			IEnumerable<KeyValuePair<string, object>> combinedData = Data;
			foreach (KeyValuePair<string, object> data in combinedData)
			{
				if (!data.Key.StartsWith("_"))	// TODO: Store const/static list of members of type Artefact, to exclude here
				{
					MemberInfo member = typeof(T).GetMember(data.Key)
						.FirstOrDefault(mi => mi.MemberType == MemberTypes.Field ||
							(mi.MemberType == MemberTypes.Property && (((PropertyInfo)mi).GetSetMethod() != null)));
					if (member == null)
						throw new MissingMemberException(typeof(T).FullName, data.Key);
					object value = data.Value;
					Type valueType = value == null ? typeof(System.Object) : value.GetType();
					Type memberType = member.GetMemberReturnType();
					if (memberType.IsEnum)
						value = Enum.ToObject(memberType, value);
					// Needs to be else if?
					if (!memberType.IsAssignableFrom(valueType))
						value = Convert.ChangeType(value, memberType);
					member.SetValue(instance, value);
				}
			}
			return instance;
		}
		
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="Artefacts.Artefact"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Artefacts.Artefact"/>.</returns>
		/// <remarks><see cref="System.Object"/> override</remarks>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("[Artefact:");
			IEnumerable<KeyValuePair<string, object>> combinedData = Data;
			foreach (KeyValuePair<string, object> field in combinedData)
				sb.AppendFormat(" {0}={1}", field.Key, field.Value);
			return sb.Append("]").ToString();
		}
		#endregion
	}
}
		/// <summary>
		/// Converts this object to a BsonDocument.
		/// </summary>
		/// <returns>A <see cref="BsonDocument"/></returns>
		/// <remarks><see cref="IConvertibleToBsonDocument"/> implementation</remarks>
//		public BsonDocument ToBsonDocument()
//		{
//			BsonDocument document = new BsonDocument();
//			IEnumerable<KeyValuePair<string, object>> combinedData = Data;
//			foreach (KeyValuePair<string, object> data in combinedData)
//			{
//				object value = data.Value;
//				Type valueType = value != null ? value.GetType() : typeof(object);
//				BsonValue bsonValue;
//				if (valueType == typeof(long)) 
//					bsonValue = BsonInt64.Create(value);
//				else if (valueType == typeof(int))
//					bsonValue = BsonInt32.Create(value);
//				else if (valueType.IsEnum)
////					bsonValue = value.ToString(
//					bsonValue = BsonInt32.Create((int)value);
//				else
//					bsonValue = BsonValue.Create(value);
//				document.Add(data.Key, bsonValue);
//			}
//			return document;
//		}

//		#region DynamicObject overrides
//		/// <summary>
//		/// Gets the dynamic member names.
//		/// </summary>=
//		/// <returns>The dynamic member names.</returns>
//		public override IEnumerable<string> GetDynamicMemberNames()
//		{
//			List<string> names = new List<string>(Data.Keys);
//			names.AddRange(PersistedData.Keys);
//			return names;
//		}
//
//		/// <summary>
//		/// Tries the get member.
//		/// </summary>
//		/// <param name="binder">Binder.</param>
//		/// <param name="result">Result.</param>
//		/// <returns><c>true</c>, if get member was tryed, <c>false</c> otherwise.</returns>
//		public override bool TryGetMember(GetMemberBinder binder, out object result)
//		{
//			return Data.TryGetValue(binder.Name, out result) || PersistedData.TryGetValue(binder.Name, out result);
//		}
//
//		/// <summary>
//		/// Tries the set member.
//		/// </summary>
//		/// <returns><c>true</c>, if set member was tryed, <c>false</c> otherwise.</returns>
//		/// <param name="binder">Binder.</param>
//		/// <param name="value">Value.</param>
//		/// <remarks>
//		/// Would this be the right spot to track/create (am i creating?) an object graph
//		/// ie you would (if value is a non-primitive class type) create a new Artefact
//		/// in this method 
//		/// </remarks>
//		public override bool TrySetMember(SetMemberBinder binder, object value)
//		{
//			try {	// not sure if I really need this, just randomly thought i'd try it out?
//				if (Data.ContainsKey(binder.Name) && Data[binder.Name] != value)
//					State = ArtefactState.Modified;
//				Data[binder.Name] = value;
//			}
//			catch (Exception ex)
//			{
//				return false;
//			}
//			return true;
//		}
//
//		/// <summary>
//		/// Tries the create instance.
//		/// </summary>
//		/// <returns><c>true</c>, if create instance was tryed, <c>false</c> otherwise.</returns>
//		/// <param name="binder">Binder.</param>
//		/// <param name="args">Arguments.</param>
//		/// <param name="result">Result.</param>
//		/// <remarks>
//		/// Is this where I could put a call to storage->add(object) instead of storage->update/save()
//		/// because this method called when a new Artefact is created?
//		/// 		ie a = new Artefact(typed value client side)
//		/// </remarks>
//		public override bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result)
//		{
//			result = args.Length == 1 ? new Artefact(args[0]) : new Artefact();
//			return true;
//		}
//
//		/// <summary>
//		/// Tries the convert.
//		/// </summary>
//		/// <returns><c>true</c>, if convert was tryed, <c>false</c> otherwise.</returns>
//		/// <param name="binder">Binder.</param>
//		/// <param name="result">Result.</param>
//		public override bool TryConvert(ConvertBinder binder, out object result)
//		{
//			return base.TryConvert(binder, out result);
//		}
//		#endregion
		

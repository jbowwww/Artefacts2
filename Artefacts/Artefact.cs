using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Dynamic;
using ServiceStack.Logging;
using ServiceStack;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Linq;
using System.Collections.Concurrent;
using ServiceStack.Text;
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
	[Route("/Artefacts/{Collection}/{Id}/", "POST,PUT")]
//	[DataContract]
	public class Artefact : DynamicObject, IReturn	//, IConvertibleToBsonDocument	//, IDictionary<string, object>
	{
		#region Static members
		public static readonly ILogFactory LogFactory;
		public static readonly ILog Log;
		public static TextWriter LogWriter { get; private set; }
		
		public class ArtefactTypedInstanceCache : ConcurrentDictionary<Type, object>
		{
			public Artefact Artefact { get; internal set; }
			
			public T GetInstance<T>()
			{
				Type type = typeof(T);
				if (base.ContainsKey(type))
					return (T)base[type];
				T instance = (T)Activator.CreateInstance(type);
				base[type] = instance;
				return instance;
			}
			
			internal ArtefactTypedInstanceCache(Artefact artefact) : base(4, 4)
			{
				if (artefact == null)
					throw new ArgumentNullException("artefact");
				Artefact = artefact;
			}
		}
		
		public class ArtefactCache
		{
			public ConcurrentDictionary<string, ArtefactTypedInstanceCache> InstancesFromArtefact = new ConcurrentDictionary<string, ArtefactTypedInstanceCache>();
			public ConcurrentDictionary<object, Artefact> ArtefactFromInstance = new ConcurrentDictionary<object, Artefact>();
			
			public Artefact GetArtefact(object instance)
			{
				Artefact artefact;
				if (ArtefactFromInstance.ContainsKey(instance))
				{
					artefact = ArtefactFromInstance[instance];
					artefact.SetInstance(instance);
				}
				else
				{
					artefact = new Artefact(instance);
					ArtefactFromInstance[instance] = artefact;
				}
				return artefact;
			}

			public ArtefactTypedInstanceCache GetTypedInstanceCache(Artefact artefact)
			{
				return (ArtefactTypedInstanceCache)InstancesFromArtefact.GetOrAdd(artefact.Id, (_artefactId) => new ArtefactTypedInstanceCache(artefact));
			}
			
			public T GetInstance<T>(Artefact artefact)
			{
				T instance;
				ArtefactTypedInstanceCache cache = GetTypedInstanceCache(artefact);
				return cache.GetInstance<T>();
			}
		}
		
		public static readonly ArtefactCache Cache = new ArtefactCache();

		/// <summary>
		/// Initializes the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		static Artefact()
		{
			LogFactory = new ConsoleLogFactory();
			Log = Artefact.LogFactory.GetLogger(typeof(Artefact));
		}
		
		/// <summary>
		/// Configures service stack - serialisers etc
		/// </summary>
		public static void ConfigureServiceStack()
		{
			
			MongoDB.Bson.IO.JsonWriterSettings _jsonSettings =
				new MongoDB.Bson.IO.JsonWriterSettings()
			{
				OutputMode = MongoDB.Bson.IO.JsonOutputMode.Strict
			};
			MongoDB.Bson.Serialization.IBsonSerializationOptions _serializationOptions =
				MongoDB.Bson.Serialization.Options.DictionarySerializationOptions.Document;
			
			JsConfig.ThrowOnDeserializationError = true;
			//JsConfig<Artefact>.IncludeTypeInfo = true;
			JsConfig.IncludeNullValues = false;
			JsConfig.IncludeDefaultEnums = true;
			JsConfig.TreatEnumAsInteger = true;
			JsConfig.TryToParsePrimitiveTypeValues = true;
			JsConfig.TryToParseNumericType = true;
			JsConfig.ParsePrimitiveIntegerTypes =
				ParseAsType.Byte | ParseAsType.Decimal |
					ParseAsType.Single | ParseAsType.Double |
					ParseAsType.Int32 | ParseAsType.Int64 |
					ParseAsType.UInt32 | ParseAsType.UInt64;
			JsConfig.ParsePrimitiveFloatingPointTypes =
				ParseAsType.Single |
				ParseAsType.Double |
				ParseAsType.Decimal;

			JsConfig<Artefact>.SerializeFn = a => a.Data.ToJson<DataDictionary>(_serializationOptions, _jsonSettings);
			JsConfig<Artefact>.DeSerializeFn = a => new Artefact(BsonSerializer.Deserialize<DataDictionary>(a));
			
//			JsConfig<DataDictionary>.SerializeFn = a => a.ToJson<DataDictionary>(_serializationOptions, _jsonSettings);
//			JsConfig<DataDictionary>.DeSerializeFn = a => BsonSerializer.Deserialize<DataDictionary>(a);
			//			
//						JsConfig<DataDictionary>.SerializeFn = a => StringExtensions.ToJson(a).EncodeJson();
//						JsConfig<DataDictionary>.DeSerializeFn = a => a.UrlDecode().FromJson<DataDictionary>();
			//			JsConfig<BsonDocument>.SerializeFn = b => MongoDB.Bson.BsonExtensionMethods.ToJson(b);
			//			JsConfig<BsonDocument>.DeSerializeFn = b => BsonDocument.Parse(b);
			//
			//			JsConfig.TryToParsePrimitiveTypeValues = true;
			//			JsConfig.TreatEnumAsInteger = true;
			//JsConfig<Artefact>.IncludeTypeInfo = true;
			//			JsConfig.TryToParseNumericType = true;
			//
			//			JsConfig<Artefact>.SerializeFn = a => a.Data.ToJson<DataDictionary>(_serializationOptions, _jsonSettings);
			//			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { Data = BsonSerializer.Deserialize<DataDictionary>(a) };
			//			JsConfig<BsonDocument>.SerializeFn = b => MongoDB.Bson.BsonExtensionMethods.ToJson(b);
			//			JsConfig<BsonDocument>.DeSerializeFn = b => BsonDocument.Parse(b);		
			//			
			// Old
			//			JsConfig<Artefact>.SerializeFn = a => StringExtensions.ToJsv<DataDictionary>(a./*Persisted*/Data);	// a.Data.ToJson();	// TypeSerializer.SerializeToString<DataDictionary>(a.Data);	// a.Data.SerializeToString();
			//			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { Data = a.FromJsv<DataDictionary>() };	// TypeSerializer.DeserializeFromString<DataDictionary>(a) };//.FromJson<DataDictionary>() };
			//			JsConfig<BsonDocument>.SerializeFn = b => MongoDB.Bson.BsonExtensionMethods.ToJson(b);
			//			JsConfig<BsonDocument>.DeSerializeFn = b => BsonDocument.Parse(b);
			//			JsConfig<QueryRequest>.SerializeFn = b => MongoDB.Bson.BsonExtensionMethods.ToJson<QueryRequest>(b);// b.ToJson();
			//			JsConfig<QueryRequest>.DeSerializeFn = b => (QueryRequest)BsonDocument.Parse(b);

			//			_artefactsClient = new ArtefactsClient(_serviceBaseUrl, _bufferWriter);
			//			_bufferWriter.WriteLine("_artefactsClient: {0}", _artefactsClient);
		}
		
		/// <summary>
		/// Makes the name of the safe collection.
		/// </summary>
		/// <returns>The safe collection name.</returns>
		/// <param name="collectionName">Collection name.</param>
		public static string MakeSafeCollectionName(string collectionName)
		{
			return collectionName.Replace(".", "_").Replace("`", "-").Replace('[', '-').Replace(']', '-');
		}
		
		/// <summary>
		/// Defaults member filter.
		/// </summary>
		/// <returns><c>true</c>, if member filter was _defaulted, <c>false</c> otherwise.</returns>
		/// <param name="member">Member.</param>
		public static bool DefaultMemberFilter(MemberInfo member)
		{
			return
				(member.MemberType == MemberTypes.Field ||
				 member.MemberType == MemberTypes.Property)
			 &&  member.IsPublic()
			 && (member.GetCustomAttribute<IgnoreDataMemberAttribute>() == null);
		}
		
		public delegate bool IncludeMemberDelegate(MemberInfo member);
		public static IncludeMemberDelegate MemberFilter = DefaultMemberFilter;
		
		public static BindingFlags DefaultBindingFlags =
			BindingFlags.Instance |
			BindingFlags.Public | BindingFlags.NonPublic | 
			BindingFlags.GetField | BindingFlags.GetProperty;
		
		public static IEnumerable<MemberInfo> GetDataMembers(Type instanceType, BindingFlags bindingFlags = 0, IncludeMemberDelegate memberFilter = null)
		{
			if (instanceType == null)
				throw new ArgumentNullException("instanceType");
			if (bindingFlags == 0)
				bindingFlags = DefaultBindingFlags;
			if (memberFilter == null)
				memberFilter = MemberFilter;
			return instanceType.GetMembers(bindingFlags).Where(mi => memberFilter(mi));
		}
		#endregion
		
		#region Private fields
		private DataDictionary _artefactData;
//		private DataDictionary _persistedData;
		private string _uri;
//		private ArtefactsClient _client = null;
		private ArtefactTypedInstanceCache _instanceCache;
		#endregion
		
		#region Fields & Properties
		/// <summary>
		/// Gets or sets the artefact's identifier.
		/// </summary>
//		[BsonId, DataMember]
		public string Id {
			get { return (string)Data["_id"]; }
			set { Data["_id"] = value/*.ToString()*/; }
		}

		/// <summary>
		/// Gets or sets the artefact's URI
		/// </summary>
//		[BsonIgnore, DataMember]
//		public string Uri {
//			get { return _uri ?? (_uri = PathUtils.CombinePaths("/", Collection, Id)); }
//		}
		
		/// <summary>
		/// Gets or sets the server-side collection name
		/// </summary>
//		[BsonIgnore, DataMember]
		public string Collection {
			get;// { return (string)this["_collection"]; }
			set;// { this["_collection"] = value; }
		}

		/// <summary>
		/// Gets the <see cref="ArtefactState"/> of this artefact
		/// </summary>
//		[BsonIgnore]	//, DataMember]
		public ArtefactState State {
			get;
			set;
		}
		
		/// <summary>
		/// Gets or sets the time created.
        /// </summary>
//		[BsonIgnore]	//BsonElement("_timeCreated"), DataMember]
		public DateTime TimeCreated {
			get { return (DateTime/*.Parse((string*/)this["_timeCreated"]; }
			set { this["_timeCreated"] = value/*.ToString()*/; }
		}

		/// <summary>
		/// Gets or sets the time checked.
		/// </summary>
//		[BsonIgnore]
		//BsonElement("_timeChecked"), BsonRepresentation(BsonType.String), DataMember]
		public DateTime TimeChecked {
			get { return (DateTime/*.Parse((string*/)this["_timeChecked"]; }
			set { this["_timeChecked"] = value/*.ToString()*/; }
		}

		/// <summary>
		/// Gets or sets the time modified.
		/// </summary>
//		[BsonIgnore]
		//BsonElement("_timeModified"), BsonRepresentation(BsonType.String), DataMember]
		public DateTime TimeModified {
			get { return (DateTime/*.Parse((string*/)this["_timeModified"]; }
			set { this["_timeModified"] = value/*.ToString()*/; }
		}

		/// <summary>
		/// Timestamp when last this <see cref="Artefact"/> was saved/sent to server
		/// </summary>
//		[BsonIgnore]
		//BsonElement("_timeSaved"), BsonRepresentation(BsonType.String), DataMember]
		public DateTime TimeSaved {
			get { return (DateTime/*.Parse((string*/)this["_timeSaved"]; }
			set { this["_timeSaved"] = value/*.ToString()*/; }
		}
		
		/// <summary>
		/// Gets or sets the artefact data.
		/// </summary>
		[BsonExtraElements]//, DataMember]
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
			get { return Data[name]; }
			set { Data[name] = value; }
		}
		#endregion

		#region Construction / Initialisation
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		protected Artefact(DataDictionary data)
		{
			_artefactData = data;
//			_instanceCache = new ArtefactTypedInstanceCache(Id);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		/// <param name="instance">Instance.</param>
		public Artefact(object instance)
		{
			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
			TimeSaved = DateTime.MinValue;
			State = ArtefactState.Unknown;
			Id = ObjectId.GenerateNewId(TimeCreated).ToString();
			State = ArtefactState.Created;
//			_client = client;
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
		/// Gets the member.
		/// </summary>
		/// <returns>The member.</returns>
		/// <param name="name">Name.</param>
		public object GetDataMember(string name)
		{
			return Data[name];
		}	

		/// <summary>
		/// Sets the data member.
		/// </summary>
		/// <returns>The data member.</returns>
		/// <param name="name">Name.</param>
		public void SetDataMember(string name, object value)
		{
			Data[name] = value;
		}
		
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
		public Artefact SetInstance(object instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
			Type instanceType = instance.GetType();
			Collection = MakeSafeCollectionName(instanceType.FullName);
			IEnumerable<MemberInfo> members = GetDataMembers(instanceType);
			foreach (MemberInfo member in members)
			{
				object value = member.GetValue(instance);	// .GetPropertyOrField(instance);
//				Type valueType = value != null ? value.GetType() : typeof(object);
				//				if (value != null && valueType.IsClass()
				//				 &&	valueType != typeof(string)
				//				 &&	valueType != typeof(DateTime)
				//				 &&	valueType != typeof(TimeSpan))
				//					Data[member.Name] = new Artefact(value);
				//				else
				Data[member.Name] = value;
			}
			Artefact.Cache.GetTypedInstanceCache(this)[instanceType] = instance;
			Artefact.Cache.ArtefactFromInstance[instance] = this;
			return this;
//			return Data.Count;
		}
		
		/// <summary>
		/// As this instance.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T As<T>() //where T : new()
		{
			T instance = Artefact.Cache.GetInstance<T>(this);	//Activator.CreateInstance<T>(); //	new T();
			MemberInfo[] typeMembers = GetDataMembers(typeof(T)).ToArray();
			IEnumerable<KeyValuePair<string, object>> combinedData = Data;
			foreach (KeyValuePair<string, object> data in combinedData)
			{
				if (!data.Key.StartsWith("_"))	// TODO: Store const/static list of members of type Artefact, to exclude here
				{								// ^ Above lien not currently eneded if yous tay with keeping Time* properties separate from this.Data
					MemberInfo member = typeMembers.FirstOrDefault(mi => mi.Name == data.Key);
					if (member == null)
						Log.DebugFormat("Member '{0}' not found in type '{1}', skipping", data.Key, typeof(T).FullName);
						//throw new MissingMemberException("", /*typeof(T).FullName,*/ data.Key);
					else
					{
						object value = data.Value;
						Type valueType = value == null ? typeof(System.Object) : value.GetType();
						Type memberType = member.GetMemberReturnType();
						if (!memberType.IsAssignableFrom(valueType))
						{
							if (memberType.IsEnum)
							{
								if (valueType == typeof(string))
									value = Enum.Parse(memberType, (string)value, true);
								else
									value = Enum.ToObject(memberType, value);
//								throw new ArgumentOutOfRangeException("value", valueType,
//									"Could not convert value of type " + valueType.FullName +
//									" to Enum type " + memberType.FullName);
							}
							else if (memberType.IsNullableType())	// Not sure this is needed - I think a value type and its Nullable<> equiv are assignable
								value = Activator.CreateInstance(memberType, new object[] { value });
							else
								value = Convert.ChangeType(value, memberType);
						}
						member.SetValue(instance, value);
					}
				}
			}
			Artefact.Cache.ArtefactFromInstance[instance] = this;
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
				sb.AppendFormat(" {0}={1}", field.Key, field.Value ?? "(null)");
			return sb.Append("]").ToString();
		}

		/// <summary>
		/// Converts this object to a BsonDocument.
		/// </summary>
		/// <returns>A <see cref="BsonDocument"/></returns>
		/// <remarks><see cref="IConvertibleToBsonDocument"/> implementation</remarks>
		//	public BsonDocument ToBsonDocument()
		//	{
		//		BsonDocument document = new BsonDocument();
		//		IEnumerable<KeyValuePair<string, object>> combinedData = Data;
		//		foreach (KeyValuePair<string, object> data in combinedData)
		//		{
		//			object value = data.Value;
		//			Type valueType = value != null ? value.GetType() : typeof(object);
		//			BsonValue bsonValue;
		//			if (valueType == typeof(long)) 
		//				bsonValue = BsonInt64.Create(value);
		//			else if (valueType == typeof(int))
		//				bsonValue = BsonInt32.Create(value);
		//			else if (valueType.IsEnum)
		//				//bsonValue = value.ToString(
		//				bsonValue = BsonInt32.Create((int)value);
		//			else
		//				bsonValue = BsonValue.Create(value);
		//			document.Add(data.Key, bsonValue);
		//		}
		//		return document;
		//	}
		
		#region DynamicObject overrides
		/// <summary>
		/// Gets the dynamic member names.
		/// </summary>=
		/// <returns>The dynamic member names.</returns>
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			List<string> names = new List<string>(Data.Keys);
//			names.AddRange(PersistedData.Keys);
			return names;
		}

		/// <summary>
		/// Tries the get member.
		/// </summary>
		/// <param name="binder">Binder.</param>
		/// <param name="result">Result.</param>
		/// <returns><c>true</c>, if get member was tryed, <c>false</c> otherwise.</returns>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			return Data.TryGetValue(binder.Name, out result);// || PersistedData.TryGetValue(binder.Name, out result);
		}

		/// <summary>
		/// Tries the set member.
		/// </summary>
		/// <returns><c>true</c>, if set member was tryed, <c>false</c> otherwise.</returns>
		/// <param name="binder">Binder.</param>
		/// <param name="value">Value.</param>
		/// <remarks>
		/// Would this be the right spot to track/create (am i creating?) an object graph
		/// ie you would (if value is a non-primitive class type) create a new Artefact
		/// in this method 
		/// </remarks>
		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			try
			{	// not sure if I really need this, just randomly thought i'd try it out?
				if (Data.ContainsKey(binder.Name) && Data[binder.Name] != value)
					State = ArtefactState.Modified;
				Data[binder.Name] = value;
			}
			catch (Exception ex)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Tries the create instance.
		/// </summary>
		/// <returns><c>true</c>, if create instance was tryed, <c>false</c> otherwise.</returns>
		/// <param name="binder">Binder.</param>
		/// <param name="args">Arguments.</param>
		/// <param name="result">Result.</param>
		/// <remarks>
		/// Is this where I could put a call to storage->add(object) instead of storage->update/save()
		/// because this method called when a new Artefact is created?
		/// 		ie a = new Artefact(typed value client side)
		/// </remarks>
		public override bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result)
		{
			if (args.Length == 1)
				result = new Artefact(args[0]);
			else
				throw new InvalidOperationException("Artefact.TryCreateInstance(): args.Length=" + args.Length + " != 1");
			return true;
		}

		/// <summary>
		/// Tries the convert.
		/// </summary>
		/// <returns><c>true</c>, if convert was tryed, <c>false</c> otherwise.</returns>
		/// <param name="binder">Binder.</param>
		/// <param name="result">Result.</param>
		public override bool TryConvert(ConvertBinder binder, out object result)
		{
			return base.TryConvert(binder, out result);
		}
		#endregion
		#endregion
	}
}
		

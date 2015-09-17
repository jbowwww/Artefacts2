using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization;
using ServiceStack.Text.Json;
using MongoDB.Bson;
using System.Collections;
using MongoDB.Bson.Serialization.Attributes;
using ServiceStack;
using System.Text;
using ServiceStack.Text;

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
<<<<<<< HEAD
//	[DataContract]
//	[CollectionDataContract]
//	[Route("/{Collection}/{Name}")]
	public class Artefact : DynamicObject//, IDictionary<string, object>
=======
	[DataContract]
	public class Artefact : DynamicObject, IConvertibleToBsonDocument, IReturn<object>
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
	{	
		#region Fields & Properties
		[NonSerialized] private BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty;
		[NonSerialized] private ArtefactData _artefactData;
		
		/// <summary>
		/// Gets or sets the artefact data.
		/// </summary>
//		[BsonExtraElements]
		public ArtefactData Data {
			get { return _artefactData ?? (_artefactData = new ArtefactData()); }
			set { _artefactData = new ArtefactData(value); }
		}
				
		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		[BsonId, BsonRepresentation(BsonType.String)]
		public string Id {
<<<<<<< HEAD
			get { return (string)(Data.ContainsKey("_id") ? Data["_id"] : string.Empty); }	//Data["_id"]; }
			set { Data["_id"] = value; }
		}

//		[DataMember(Order = 4)]
		public string Uri {
			get
			{
				return _uri ?? (_uri = PathUtils.CombinePaths(Collection, Name));
			}
			set
			{
				Name = value.Substring(value.IndexOf('/') + 1);
				Collection = value.Substring(0, value.Length - Name.Length - 1);
			}
		}
		
		//		[DataMember(Order = 3)]
		public string Collection {
			get;
			set;
=======
			get { return (string)Data["Id"]; }
			set { Data["Id"] = value; }
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
		}

		/// <summary>
		/// Gets the "primary key" data member in the artefact, used to test artefacts already in the repo
		/// with client instances for equality/equivalence.
		/// </summary>
		public object Key {
			get
			{
				return
						Data.ContainsKey("Name") ? Data["Name"]
					:	Data.ContainsKey("name") ? Data["name"]
					:	null;
			}
<<<<<<< HEAD
			set
			{
				if (value != Name)
					throw new ArgumentOutOfRangeException("value", value, "Should match field in Data");
			}
		}
		
		/// <summary>
		/// Gets the <see cref="ArtefactState"/> of this artefact
		/// </summary>
		public ArtefactState State {
			get;
			private set;
=======
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
		}
		
		/// <summary>
		/// Gets or sets the time created.
		/// </summary>
		[BsonRequired]
		public DateTime TimeCreated {
			get { return (DateTime)Data["TimeCreated"]; }
			set { Data["TimeCreated"] = value; }
		}

		/// <summary>
		/// Gets or sets the time checked.
		/// </summary>
		[BsonRequired]
		public DateTime TimeChecked {
			get { return (DateTime)Data["TimeChecked"]; }
			set { Data["TimeChecked"] = value; }
		}

		/// <summary>
		/// Gets or sets the time modified.
		/// </summary>
		[BsonRequired]
		public DateTime TimeModified {
			get { return (DateTime)Data["TimeModified"]; }
			set { Data["TimeModified"] = value; }
		}
		#endregion

		#region Construction / Initialisation
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		[OnDeserializing]
		public void OnDeserializing()
		{
			Data = new ArtefactData();
			Id = ObjectId.GenerateNewId().ToString();
			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
		}
				
		public Artefact()
		{
<<<<<<< HEAD
			State = ArtefactState.Unknown;
			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
			Id = ObjectId.GenerateNewId(TimeCreated).ToString();
				// ^ if this is only used when d's'ing S'side won't this new ID be useless??
=======
			Data = new ArtefactData();
			Id = ObjectId.GenerateNewId().ToString();
			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		/// <param name="instance">Instance.</param>
		public Artefact(object instance = null) : this()
		{
<<<<<<< HEAD
=======
			Id = ObjectId.GenerateNewId().ToString();
			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
			if (instance == null)
				throw new ArgumentNullException("value");
			if (!instance.GetType().IsClass)
				throw new ArgumentOutOfRangeException("value", "Not a class type");
			if (instance != null)
				SetInstance(instance);
			State = ArtefactState.Created;
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
		private int SetInstance(object instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
			foreach (MemberInfo member in instance.GetType().GetMembers(_bindingFlags)
			         .Where((mi) =>
			       mi.MemberType == MemberTypes.Field ||
			       mi.MemberType == MemberTypes.Property))
			{
				object value = member.GetPropertyOrField(instance);
				Type valueType = value.GetType();
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
<<<<<<< HEAD
		#endregion

		#region Data handling
		private void OnSerializing()
		{
			State = ArtefactState.Serializing;
		}
		
		/// <summary>
		/// Raises the serialized event.
		/// </summary>
		[OnSerialized]
		private void OnSerialized()
		{
			PersistedData.AddValues(Data);
			Data.Clear();
			State = ArtefactState.Current;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		[OnDeserializing]
		private void OnDeserializing()
		{
			State = ArtefactState.Deserializing;
//			Data = new DataDictionary();
//			PersistedData = new DataDictionary();
			//			Id = ObjectId.GenerateNewId().ToString();
			//			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
		}

		/// <summary>
		/// Raises the deserialized event.
		/// </summary>
		[OnDeserialized]
		private void OnDeserialized()
		{
			State = ArtefactState.Current;
		}	
=======
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
		
		/// <summary>
		/// Serializes the and format.
		/// </summary>
<<<<<<< HEAD
		/// <returns>The and format.</returns>
		public string SerializeAndFormat()
		{
			return ServiceStack.Text.TypeSerializer.SerializeToString<Artefact>(this);
		}
		
		/// <summary>
		/// As this instance.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T As<T>() where T : new()
		{
			T instance = new T();
			foreach (KeyValuePair<string, object> data in Data)
			{
				if (!data.Key.StartsWith("_"))
				{
					MemberInfo[] mi = typeof(T).GetMember(data.Key);
					if (mi == null || mi.Length == 0)
						throw new MissingMemberException(typeof(T).FullName, data.Key);
					mi[0].SetValue(instance, data.Value);
				}
			}
			return instance;
		}

		/// <summary>
		/// Converts this object to a BsonDocument.
		/// </summary>
		/// <returns>A <see cref="BsonDocument"/></returns>
		/// <remarks><see cref="IConvertibleToBsonDocument"/> implementation</remarks>
		public BsonDocument ToBsonDocument()
		{
			return new BsonDocument(Data);
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="Artefacts.Artefact"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Artefacts.Artefact"/>.</returns>
		/// <remarks><see cref="System.Object"/> override</remarks>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("[Artefact:");
			foreach (KeyValuePair<string, object> field in Data)
				sb.AppendFormat(" {0}={1}", field.Key, field.Value);
			return sb.Append("]").ToString();
		}
		#endregion

		#region IDictionary implementation
		/// <summary>
		/// Add the specified key and value.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		public void Add(string key, object value)
		{
			Data.Add(key, value);
		}

		/// <Docs>The key to locate in the current instance.</Docs>
		/// <para>Determines whether the current instance contains an entry with the specified key.</para>
		/// <summary>
		/// Containses the key.
		/// </summary>
		/// <returns><c>true</c>, if key was containsed, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		public bool ContainsKey(string key)
		{
			return Data.ContainsKey(key) || PersistedData.ContainsKey(key);
		}

		/// <Docs>The item to remove from the current collection.</Docs>
		/// <para>Removes the first occurrence of an item from the current collection.</para>
		/// <summary>
		/// Remove the specified key.
		/// </summary>
		/// <param name="key">Key.</param>
		public bool Remove(string key)
		{
			return Data.Remove(key) || PersistedData.Remove(key); 
		}

		/// <Docs>To be added.</Docs>
		/// <summary>
		/// To be added.
		/// </summary>
		/// <remarks>To be added.</remarks>
		/// <returns><c>true</c>, if get value was tryed, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		public bool TryGetValue(string key, out object value)
		{
			return Data.TryGetValue(key, out value) || PersistedData.TryGetValue(key, out value);
		}

		/// <summary>
		/// Gets or sets the <see cref="Artefacts.Artefact"/> at the specified index.
		/// </summary>
		/// <param name="index">Index.</param>
		public object this[string index] {
			get { return Data.ContainsKey(index) ? Data[index] : PersistedData[index]; }
			set { Data[index] = value; }
		}

		/// <summary>
		/// Gets the keys.
		/// </summary>
		/// <value>The keys.</value>
		public ICollection<string> Keys {
			get {
				List<string> keys = new List<string>(Data.Keys);
				keys.AddRange(PersistedData.Keys);
				return keys;
			}
		}

		/// <summary>
		/// Gets the values.
		/// </summary>
		/// <value>The values.</value>
		public ICollection<object> Values {
			get {
				List<object> values = new List<object>(Data.Values);
				values.Add(PersistedData.Values);
				return values;
			}
		}
		#endregion

		#region ICollection implementation
		/// <Docs>The item to add to the current collection.</Docs>
		/// <para>Adds an item to the current collection.</para>
		/// <remarks>To be added.</remarks>
		/// <exception cref="System.NotSupportedException">The current collection is read-only.</exception>
		/// <summary>
		/// Add the specified item.
		/// </summary>
		/// <param name="item">Item.</param>
		public void Add(KeyValuePair<string, object> item)
		{
			Data.Add(item.Key, item.Value);
		}

		/// <summary>
		/// Clear this instance.
		/// </summary>
		public void Clear()
		{
			Data.Clear();
			PersistedData.Clear();
		}

		/// <Docs>The object to locate in the current collection.</Docs>
		/// <para>Determines whether the current collection contains a specific value.</para>
		/// <summary>
		/// Contains the specified item.
		/// </summary>
		/// <param name="item">Item.</param>
		public bool Contains(KeyValuePair<string, object> item)
		{
			return Data.Contains(item) || PersistedData.Contains(item);
		}

		/// <summary>
		/// Copies to.
		/// </summary>
		/// <param name="array">Array.</param>
		/// <param name="arrayIndex">Array index.</param>
		public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<string, object>>)Data).CopyTo(array, arrayIndex);
			((ICollection<KeyValuePair<string, object>>)PersistedData).CopyTo(array, arrayIndex + Data.Count);
		}

		/// <Docs>The item to remove from the current collection.</Docs>
		/// <para>Removes the first occurrence of an item from the current collection.</para>
		/// <summary>
		/// Remove the specified item.
		/// </summary>
		/// <param name="item">Item.</param>
		public bool Remove(KeyValuePair<string, object> item)
		{
			return Data.Remove(item.Key) || PersistedData.Remove(item.Key);
		}

		/// <summary>
		/// Gets the count.
		/// </summary>
		/// <value>The count.</value>
		public int Count {
			get { return Data.Count + PersistedData.Count; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is read only.
		/// </summary>
		/// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
		public bool IsReadOnly {
			get { return false; }
		}
		#endregion

		#region IEnumerable implementation
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			return Data.Concat(PersistedData).GetEnumerator();
		}
		
		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)GetEnumerator();
		}
		#endregion
		
=======
		/// <returns>The instance.</returns>
		/// <param name="instance">Instance.</param>
		public int SetInstance(object instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
			foreach (MemberInfo member in instance.GetType().GetMembers(_bindingFlags)
			         .Where((mi) =>
			       mi.MemberType == MemberTypes.Field ||
			       mi.MemberType == MemberTypes.Property))
				Data[member.Name] = (member.GetPropertyOrField(instance)).ToString();
			return Data.Count;
		}
		#endregion

		#region Methods
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
		#region DynamicObject overrides
		/// <summary>
		/// Gets the dynamic member names.
		/// </summary>
		/// <returns>The dynamic member names.</returns>
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return Data.Keys;
		}

		/// <summary>
		/// Tries the get member.
		/// </summary>
		/// <param name="binder">Binder.</param>
		/// <param name="result">Result.</param>
		/// <returns><c>true</c>, if get member was tryed, <c>false</c> otherwise.</returns>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			return Data.TryGetValue(binder.Name, out result);
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
			Data[binder.Name] = value;
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
			result = args.Length == 1 ? new Artefact(args[0]) : new Artefact();
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
		
		#region Serialization / data handling
		/// <summary>
		/// Converts this object to a BsonDocument.
		/// </summary>
		/// <returns>A <see cref="BsonDocument"/></returns>
		/// <remarks><see cref="IConvertibleToBsonDocument"/> implementation</remarks>
		public BsonDocument ToBsonDocument()
		{
			return new BsonDocument(Data);
		}
		
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="Artefacts.Artefact"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Artefacts.Artefact"/>.</returns>
		/// <remarks><see cref="System.Object"/> override</remarks>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("[Artefact:");
			foreach (KeyValuePair<string, object> field in Data)
				sb.AppendFormat(" {0}={1}", field.Key, field.Value);
			return sb.Append("]").ToString();
		}
		#endregion
		#endregion
	}
}

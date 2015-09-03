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
//	[DataContract]
	[CollectionDataContract]
//	[Route("/{Collection}/{Name}")]
	public class Artefact : DynamicObject//, IDictionary<string, object>
	{	
		#region Fields & Properties
		private BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty;
		private DataDictionary _artefactData;
		private DataDictionary _persistedData;
		private string _uri = null;
		
		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		//		[BsonId, BsonRepresentation(BsonType.String)]
		//		[DataMember(Order = 1)]
		public string Id {
			get { return (string)Data["_id"]; }
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
		}

		/// <summary>
		/// Gets the "primary key" data member in the artefact, used to test artefacts already in the repo
		/// with client instances for equality/equivalence.
		/// </summary>
		//		[DataMember(Order = 2)]
		public string Name {
			get
			{
				return
					Data.ContainsKey("Name") ? (string)Data["Name"]
					:	Data.ContainsKey("name") ? (string)Data["name"]
					:	null;
			}
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
		}
		
		/// <summary>
		/// Gets or sets the time created.
		/// </summary>
		//		[BsonRequired]
		//		[DataMember(Order = 5)]
		public DateTime TimeCreated {
			get { return (DateTime)Data["_timeCreated"]; }
			set { Data["_timeCreated"] = value; }
		}

		/// <summary>
		/// Gets or sets the time checked.
		/// </summary>
		//		[BsonRequired]
		//		[DataMember(Order = 6)]
		public DateTime TimeChecked {
			get { return (DateTime)Data["_timeChecked"]; }
			set { Data["_timeChecked"] = value; }
		}

		/// <summary>
		/// Gets or sets the time modified.
		/// </summary>
		//		[BsonRequired]
		//		[DataMember(Order = 7)]
		public DateTime TimeModified {
			get { return (DateTime)Data["_timeModified"]; }
			set { Data["_timeModified"] = value; }
		}

		/// <summary>
		/// Gets or sets the artefact data.
		/// </summary>
//		[BsonExtraElements]
//		[DataMember(Order = 8)]
		public DataDictionary Data {
			get { return _artefactData ?? (_artefactData = new DataDictionary()); }
			set { _artefactData = new DataDictionary(value); }
		}
		
		/// <summary>
		/// Gets or sets the persisted data.
		/// </summary>
		/// <value>The persisted data.</value>
		public DataDictionary PersistedData {
			get { return _persistedData ?? (_persistedData = new DataDictionary()); }
			set { _persistedData = new DataDictionary(value); }
		}
		#endregion

		#region Construction / Initialisation
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		public Artefact()
		{
			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
			Id = ObjectId.GenerateNewId(TimeCreated).ToString();
				// ^ if this is only used when d's'ing S'side won't this new ID be useless??
			State = ArtefactState.Unknown;
			Data = new DataDictionary();
			PersistedData = new DataDictionary();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		/// <param name="instance">Instance.</param>
		public Artefact(object instance) : this()
		{
			State = ArtefactState.Created;
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
			Data = new DataDictionary();
			PersistedData = new DataDictionary();
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
		/// Serializes the and format.
		/// </summary>
		/// <returns>The and format.</returns>
		public string SerializeAndFormat()
		{
			return ServiceStack.Text.TypeSerializer.SerializeToString<Artefact>(this);
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

		/// <summary>
		/// Converts this object to a BsonDocument.
		/// </summary>
		/// <returns>A <see cref="BsonDocument"/></returns>
		/// <remarks><see cref="IConvertibleToBsonDocument"/> implementation</remarks>
		public BsonDocument ToBsonDocument()
		{
			return new BsonDocument(Data);
		}
		#endregion

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
			return Data.TryGetValue(binder.Name, out result) || PersistedData.TryGetValue(binder.Name, out result);
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
	}
}

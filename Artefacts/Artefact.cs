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
	[DataContract]
	public class Artefact : DynamicObject, IConvertibleToBsonDocument, IReturn<object>
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
			get { return (string)Data["Id"]; }
			set { Data["Id"] = value; }
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
			Data = new ArtefactData();
			Id = ObjectId.GenerateNewId().ToString();
			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		/// <param name="instance">Instance.</param>
		public Artefact(object instance = null) : this()
		{
			Id = ObjectId.GenerateNewId().ToString();
			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
			if (instance == null)
				throw new ArgumentNullException("value");
			if (!instance.GetType().IsClass)
				throw new ArgumentOutOfRangeException("value", "Not a class type");
			if (instance != null)
				SetInstance(instance);
		}
		
		/// <summary>
		/// Sets the instance.
		/// </summary>
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

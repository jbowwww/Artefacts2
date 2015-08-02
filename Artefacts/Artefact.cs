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
	public class Artefact : DynamicObject, IConvertibleToBsonDocument, IReturn<object>
	{	
		#region Fields & Properties
		[NonSerialized] private BindingFlags _bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField | BindingFlags.GetProperty;
		[NonSerialized] private Dictionary<string, object> _artefactData;
		
		/// <summary>
		/// Gets or sets the artefact data.
		/// </summary>
		[BsonExtraElements]
		public Dictionary<string, object> ArtefactData {
			get { return _artefactData ?? (_artefactData = new Dictionary<string, object>()); }
			set { _artefactData = new Dictionary<string, object>(value); }
		}
				
		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		[BsonId, BsonRepresentation(BsonType.String)]
		public string Id {
			get { return (string)ArtefactData["Id"]; }
			set { ArtefactData["Id"] = value; }
		}

		/// <summary>
		/// Gets or sets the time created.
		/// </summary>
		[BsonRequired]
		public DateTime TimeCreated {
			get { return (DateTime)ArtefactData["TimeCreated"]; }
			set { ArtefactData["TimeCreated"] = value; }
		}

		/// <summary>
		/// Gets or sets the time checked.
		/// </summary>
		[BsonRequired]
		public DateTime TimeChecked {
			get { return (DateTime)ArtefactData["TimeChecked"]; }
			set { ArtefactData["TimeChecked"] = value; }
		}

		/// <summary>
		/// Gets or sets the time modified.
		/// </summary>
		[BsonRequired]
		public DateTime TimeModified {
			get { return (DateTime)ArtefactData["TimeModified"]; }
			set { ArtefactData["TimeModified"] = value; }
		}
		#endregion

		#region Construction / Initialisation
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
//		[OnDeserializing]
		public Artefact()
		{
			ArtefactData = new Dictionary<string, object>();
			Id = ObjectId.GenerateNewId().ToString();
			TimeChecked = TimeModified = TimeCreated = DateTime.Now;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		/// <param name="info">Info.</param>
		/// <param name="context">Context.</param>
		public Artefact(SerializationInfo info, StreamingContext context) : this()
		{
			foreach (SerializationEntry data in info)
				ArtefactData[data.Name] = data.Value;
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
				ArtefactData[member.Name] = (member.GetPropertyOrField(instance)).ToString();
			return ArtefactData.Count;
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
			return ArtefactData.Keys;
		}

		/// <summary>
		/// Tries the get member.
		/// </summary>
		/// <param name="binder">Binder.</param>
		/// <param name="result">Result.</param>
		/// <returns><c>true</c>, if get member was tryed, <c>false</c> otherwise.</returns>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			return ArtefactData.TryGetValue(binder.Name, out result);
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
			ArtefactData[binder.Name] = value;
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
			result = new Artefact((SerializationInfo)args[0], (StreamingContext)args[0]);
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
			return new BsonDocument(ArtefactData);
		}
		
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="Artefacts.Artefact"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Artefacts.Artefact"/>.</returns>
		/// <remarks><see cref="System.Object"/> override</remarks>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder("[Artefact:");
			foreach (KeyValuePair<string, object> field in ArtefactData)
				sb.AppendFormat(" {0}={1}", field.Key, field.Value);
			return sb.Append("]").ToString();
		}
		#endregion
		#endregion
	}
}

using System;
using System.Collections.Generic;
using MongoDB.Bson;
using System.Dynamic;
using System.Reflection;
using System.Linq;

namespace Artefacts
{
	/// <summary>
	/// Artefact.
	/// </summary>
	/// <remarks>
	/// 
	/// </remarks>
	public class Artefact : DynamicObject, IConvertibleToBsonDocument
	{	
		#region Fields & Properties
		private BindingFlags _bindingFlags =
			BindingFlags.Public | BindingFlags.Instance |
			BindingFlags.GetField | BindingFlags.GetProperty;

		private BsonDocument _bsonDocument = new BsonDocument();
		
		/// <summary>
		/// Gets or sets the identifier.
		/// </summary>
		public ObjectId Id {
			get { return _bsonDocument["_id"].AsObjectId; }
			set { _bsonDocument["_id"] = value; }
		}

		/// <summary>
		/// Gets or sets the time created.
		/// </summary>
		public DateTime TimeCreated {
			get { return Id.CreationTime; }
			set
			{
				if (Id.CreationTime != value)
					throw new ArgumentOutOfRangeException("TimeCreated", value, "Does not match Id.CreationTime");
			}
		}

		/// <summary>
		/// Gets or sets the time checked.
		/// </summary>
		public DateTime TimeChecked {
			get { return _bsonDocument["TimeChecked"].ToLocalTime(); }
			set { _bsonDocument["TimeChecked"] = new BsonDateTime(value); }
		}

		/// <summary>
		/// Gets or sets the time modified.
		/// </summary>
		public DateTime TimeModified {
			get { return _bsonDocument["TimeModified"].ToLocalTime(); }
			set { _bsonDocument["TimeModified"] = new BsonDateTime(value); }
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		protected Artefact()
		{
			Id = ObjectId.GenerateNewId();
			TimeChecked = TimeModified = TimeCreated;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Artefact"/> class.
		/// </summary>
		/// <param name="instance">Instance.</param>
		public Artefact(object instance = null) : this()
		{
			if (instance == null)
				throw new ArgumentNullException("value");
			if (!instance.GetType().IsClass)
				throw new ArgumentOutOfRangeException("value", "Not a class type");
			if (instance != null)
				SetInstance(instance);
		}
		#endregion

		#region Methods
		/// <summary>
		/// Sets the instance.
		/// </summary>
		/// <returns>The instance.</returns>
		/// <param name="instance">Instance.</param>
		public int SetInstance(object instance)
		{
			if (instance == null)
				throw new ArgumentNullException("instance");
			foreach (MemberInfo member in instance.GetType().GetMembers(_bindingFlags).Where((mi) => mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property))
				_bsonDocument[member.Name] = BsonValue.Create(member.GetPropertyOrField(instance));
			return _bsonDocument.ElementCount;
		}
		
		/// <summary>
		/// Gets the dynamic member names.
		/// </summary>
		/// <returns>The dynamic member names.</returns>
		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return _bsonDocument.Names;
		}

		/// <summary>
		/// Tries the get member.
		/// </summary>
		/// <param name="binder">Binder.</param>
		/// <param name="result">Result.</param>
		/// <returns><c>true</c>, if get member was tryed, <c>false</c> otherwise.</returns>
		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			result = BsonTypeMapper.MapToDotNetValue(_bsonDocument[binder.Name]);
			return true;
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
			_bsonDocument[binder.Name] = BsonValue.Create(value);
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
			return base.TryCreateInstance(binder, args, out result);
		}

		/// <summary>
		/// Converts this object to a BsonDocument.
		/// </summary>
		/// <returns>A <see cref="BsonDocument"/></returns>
		/// <remarks><see cref="IConvertibleToBsonDocument"/> implementation</remarks>
		public BsonDocument ToBsonDocument()
		{
			return _bsonDocument;
		}

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="Artefacts.Artefact"/>.
		/// </summary>
		/// <returns>A <see cref="System.String"/> that represents the current <see cref="Artefacts.Artefact"/>.</returns>
		/// <remarks><see cref="System.Object"/> override</remarks>
		public override string ToString()
		{
			return string.Empty;
		}
		#endregion
	}
}

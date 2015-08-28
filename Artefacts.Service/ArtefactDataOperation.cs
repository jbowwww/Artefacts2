using System.Collections.Generic;
//using System.Collections;
using System.Runtime.Serialization;

namespace Artefacts.Service
{
	/// <summary>
	/// If this is a REST service do/should I really need this? Or does the HTTP verb
	/// implicitly define the operation?
	/// </summary>
	public enum ArtefactDataOperationType
	{
		Create,
		Read,
		Update,
		Delete
	};

	/// <summary>
	/// Saves extra members into base class <see cref="ArtefactData"/> <see cref="Dictionary`2"/>
	/// so it can be simply serialized by SS by using <see cref="CollectionDataContract"/>
	/// </summary>
	[CollectionDataContract]
	public class ArtefactDataOperation : ArtefactData
	{
		public ArtefactDataOperationType Operation {
			get { return (ArtefactDataOperationType)base["_operation"]; }
			set { base["_operation"] = value; }
		}
	
		public OperationTimestamp Timestamp {
			get { return (OperationTimestamp)base["_timestamp"]; }
			set { base["_timestamp"] = value; }
		}
		
		public ArtefactDataOperation(ArtefactDataOperationType operation, ArtefactData data)
		{
			Operation = operation;
			// TODO: Any other way to do this where members don't have to be copied to new instance?
			// And so that the client can (somehow) only send members that it needs to ie a subset of the
			// artefact's values?
			foreach (KeyValuePair<string, object> value in data)
				base.Add(value.Key, value.Value);
			Timestamp = new OperationTimestamp();
		}
	}
}

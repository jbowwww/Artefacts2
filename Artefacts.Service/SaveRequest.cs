using System;
using System.Linq;
using System.Linq.Expressions;
using ServiceStack;

using Serialize.Linq.Nodes;
using System.Runtime.Serialization;

namespace Artefacts.Service
{
	public enum SaveRequestType
	{
		Insert,
		Update,
		Upsert
	}
	
	[DataContract]
	public class SaveRequest : IReturn
	{
		[DataMember]
		public string CollectionName;
		[DataMember]
		public SaveRequestType Type;
		[DataMember]
		public DataDictionary Data;
		public SaveRequest(IArtefactCollection collection, SaveRequestType requestType, Artefact artefact)
		{
			CollectionName = collection.Name;
			Type = requestType;
			Data = artefact.Data;
		}
	}
	
	[DataContract]
	public class SaveBatchRequest : IReturn
	{
		[DataMember]
		public SaveRequest[] Items;
	}
}


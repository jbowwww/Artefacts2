using System;
using Artefacts;
using MongoDB;
using MongoDB.Bson;
using System.IO;
using System.Web;
using System.Net;
using ServiceStack;
using System.Runtime.CompilerServices;
using ServiceStack.Text;
using System.Linq.Expressions;
using Serialize.Linq.Nodes;
using Serialize.Linq.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using System.Linq;
using System.Collections.Generic;
using Artefacts.FileSystem;
using System.Collections;
using System.Runtime.Serialization;

namespace Artefacts.Service
{
	[DataContract]
	public class QueryResults
	{
		[DataMember]
		public IEnumerable<Artefact> Artefacts {
			get;
			set;
		}
		
		public QueryResults(IEnumerable<Artefact> artefacts) { Artefacts = new List<Artefact>(artefacts); }
		
		public IEnumerable<T> Get<T>() where T : new()
		{
			return Artefacts.Select(artefact => artefact.As<T>());
		}
	}
}


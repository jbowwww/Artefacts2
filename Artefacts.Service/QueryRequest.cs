using Artefacts;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System;
using ServiceStack;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using Serialize.Linq.Nodes;
using Serialize.Linq;
using Serialize.Linq.Extensions;
using Serialize.Linq.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace Artefacts.Service
{	
	[DataContract]
//	[Route("/Artefacts/", "GET")]//{CollectionName}/{QueryData}/
	public class QueryRequest<T> : QueryRequest
	{
		public QueryRequest(Expression expression) : this(typeof(T).FullName, expression) { }
		public QueryRequest(Expression<Func<T, bool>> predicate) : this((Expression)predicate) { }
		public QueryRequest(string collectionName, Expression expression)	// IMongoQuery query)
		{
			if (collectionName.IsNullOrSpace())
				throw new ArgumentOutOfRangeException("collectionName", collectionName, "collectionName is NULL or whitespace");
			ArtefactQueryTranslator<T> translator = new ArtefactQueryTranslator<T>();
			IMongoQuery query = translator.Translate(expression);
			CollectionName = Artefact.MakeSafeCollectionName(collectionName);
			Query = new QueryDocument(query.ToBsonDocument());
		}
	}
	
	[DataContract]
//	[Route("/Artefacts/", "GET")]//{CollectionName}/{QueryData}/
	public class QueryRequest : IReturn<QueryResults>//<object>
	{
		#region Static members
		public static QueryRequest Make<T>(Expression<Func<T, bool>> predicate)
		{
			return Make<T>((Expression)predicate);
		}
		
		public static QueryRequest Make<T>(Expression expression)
		{
			return Make<T>(typeof(T).FullName, expression);
		}
		
		public static QueryRequest Make<T>(string collectionName, Expression expression)
		{
			if (collectionName.IsNullOrSpace())
				throw new ArgumentOutOfRangeException("collectionName", collectionName, "collectionName is NULL or whitespace");
			ArtefactQueryTranslator<T> translator = new ArtefactQueryTranslator<T>();
			IMongoQuery query = translator.Translate(expression);
			return new QueryRequest(collectionName, query, translator.LastOperation);
		}
		#endregion
		
		#region Properties
		[DataMember(Order = 1)]
		public string CollectionName { get; set; }

		[IgnoreDataMember]
		public QueryDocument Query { //get; set; }
			get
			{
				return _query ??
					(_query =
						QueryData == "(null)" ?
					 		null :
							new QueryDocument(
								BsonDocument.Parse(
									QueryData/*.UrlDecode()*/
								)
							)
					);
			}
			set
			{
				QueryData =
					(_query = value) == null ?
						"(null)" :
						_query.ToString()/*.UrlEncode()*/;
			}
		}
		private QueryDocument _query;

		[DataMember(Order = 2)]
		public string Operation { get; set; }
		
		[DataMember(Order = 3, Name = "Query")]
		public string QueryData { get; set; }
		#endregion
		
		protected QueryRequest() { }
		
		public QueryRequest(string collectionName, IMongoQuery query, string operation)
		{
			if (collectionName.IsNullOrSpace())
				throw new ArgumentOutOfRangeException("collectionName", collectionName, "collectionName is NULL or whitespace");
//			if (query == null)
//				throw new ArgumentNullException("query");
			if (operation.IsNullOrSpace())
				throw new ArgumentOutOfRangeException("operation", operation, "operation is NULL or whitespace");
			CollectionName = Artefact.MakeSafeCollectionName(collectionName);
			Query = new QueryDocument(query.ToBsonDocument());
			Operation = operation;
		}
		
		public override string ToString()
		{
			return string.Format("[QueryRequest: CollectionName=\"{0}\" Operation=\"{1}\" Query=\"{2}\"]",
				CollectionName, Operation, QueryData);	//Query == null ? "(null)" : Query.ToString());		//QueryData={1}]", CollectionName, QueryData);
		}
	}
}


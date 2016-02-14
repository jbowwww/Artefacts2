using System;
using System.Runtime.Serialization;
using System.Linq.Expressions;
using ServiceStack;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Artefacts.Service
{
	[DataContract]
	//	[Route("/Artefacts/", "GET")]//{CollectionName}/{QueryData}/
	public class CountRequest : IReturn<CountResults>//<object>
	{
		#region Static members
		public static CountRequest Make<T>(Expression<Func<T, bool>> predicate)
		{
			return Make<T>((Expression)predicate);
		}

		public static CountRequest Make<T>(Expression expression)
		{
			return Make<T>(typeof(T).FullName, expression);
		}

		public static CountRequest Make<T>(string collectionName, Expression expression)
		{
			if (collectionName.IsNullOrSpace())
				throw new ArgumentOutOfRangeException("collectionName", collectionName, "collectionName is NULL or whitespace");
			ArtefactQueryTranslator<T> translator = new ArtefactQueryTranslator<T>();
			IMongoQuery query = translator.Translate(expression);
			return new CountRequest(collectionName, query, translator.LastOperation);
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

		protected CountRequest() { }

		public CountRequest(string collectionName, IMongoQuery query, string operation)
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
			return string.Format("[CountRequest: CollectionName=\"{0}\" Operation=\"{1}\" Query=\"{2}\"]",
			                     CollectionName, Operation, QueryData);	//Query == null ? "(null)" : Query.ToString());		//QueryData={1}]", CollectionName, QueryData);
		}
	}
}


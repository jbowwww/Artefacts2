using Artefacts;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System;
using ServiceStack;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;

namespace Artefacts.Service
{
	[DataContract]
	[Route("/{CollectionName}/Query/{QueryData}/", "GET")]
	public class QueryRequest : IReturn<QueryResults>
	{
		#region Static members
		/// <summary>
		/// Gets or sets the visitor.
		/// </summary>
		public static ExpressionVisitor Visitor { get; set; }
		
		static QueryRequest()
		{
			Visitor = new ClientQueryVisitor();
		}

		public static QueryRequest Make<T>(Expression<Func<T, bool>> predicate)
		{
			return new QueryRequest(
				Artefact.MakeSafeCollectionName(typeof(T).FullName),
				Query<T>.Where((Expression<Func<T, bool>>)Visitor.Visit(predicate)));
		}

		public static QueryRequest Make<T>(string collectionName, Expression<Func<T, bool>> predicate)
		{
			return new QueryRequest(
				Artefact.MakeSafeCollectionName(collectionName),
				Query<T>.Where((Expression<Func<T, bool>>)Visitor.Visit(predicate)));
		}
		#endregion

		#region Properties
		[DataMember(Order = 1)]
		public string CollectionName {
			get;
			set;
		}
		
		[DataMember(Order = 2)]
		public string QueryData {
			get;	// { return Query == null ? string.Empty : Query.ToString().UrlEncode(); }
			set;	// { Query = new QueryDocument(BsonDocument.Parse(value.UrlDecode())); }
		}
		
		public QueryDocument Query {
			get
			{
				return _query ??
					(_query = new QueryDocument(
						BsonDocument.Parse(
							QueryData.UrlDecode())
					));
			}
			set
			{
				QueryData = (_query = value) == null ? 
					string.Empty
				:	_query.ToString().UrlEncode();
			}
		}
		private QueryDocument _query;
		#endregion

		public QueryRequest(string collectionName, IMongoQuery query)
		{
			if (string.IsNullOrEmpty(collectionName))
				throw new ArgumentNullException("collectionName");
			if (query == null)
				throw new ArgumentNullException("query");
			CollectionName = collectionName;
			Query = (QueryDocument)query;
		}
		
		public override string ToString()
		{
			return string.Format("[QueryRequest: CollectionName={0}, Query={1}]", CollectionName, Query);
		}
	}
	
//	[Route("/Query/{Data}")]
//	public class QueryRequest<T> : QueryRequest
//	{
//		public static QueryRequest<T> Make(Expression<Func<T, bool>> predicate)
//		{
//			return new QueryRequest<T>(predicate);
//		}
//
//		public QueryRequest(Expression<Func<T, bool>> predicate)
//		{
//			this.Predicate = predicate;
//		}
//
//		public Expression<Func<T, bool>> Predicate {
//			get { return (Expression<Func<T, bool>>)base.Predicate; }
//			set { base.Predicate = value; }
//		}
//	}
}


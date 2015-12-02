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
	[Route("/Artefacts/{CollectionName}/{QueryData}/", "GET")]
	public class QueryRequest : /* ExpressionContext, */ IReturn<QueryResults>
	{
		/// <summary>
		/// Gets or sets the visitor.
		/// </summary>
		public static ExpressionVisitor Visitor { get; set; }
		
//		public static QueryRequest Parse(string json)
//		{
//			
//		}
		
		static QueryRequest()
		{
			Visitor = new ClientQueryVisitor();
		}

//		public static QueryRequest Make<T>(Expression<Func<T, bool>> predicate)
//		{
//			return new QueryRequest((LambdaExpression)predicate, Artefact.MakeSafeCollectionName(typeof(T).FullName));
		
		public static QueryRequest Make<T>(Expression<Func<T, bool>> predicate)
		{
			return new QueryRequest(Query<T>.Where(predicate), Artefact.MakeSafeCollectionName(typeof(T).FullName));
		}

//		public static QueryRequest Make<T>(string collectionName, Expression<Func<T, bool>> predicate)
//		{
//			return new QueryRequest((LambdaExpression)predicate, Artefact.MakeSafeCollectionName(collectionName));
//		}
//		
//		[DataMember(Order = 1)]
//		public string Data { get; set; }

		[DataMember(Order = 1)]
		public string CollectionName {
			get;
//			{
//				return base.GetElement("_collectionName").Value.AsString;
//			}
			set;
//			{
//				base.Set("_collectionName", new BsonString(value));
//			}
		}
		
		public QueryDocument Query {
			get;
			set;
		}
		
		[DataMember(Order = 2)]
		public string QueryData {
			get { return Query == null ? string.Empty : Query.ToString().UrlEncode(); }
			set { Query = new QueryDocument(BsonDocument.Parse(value.UrlDecode())); }
		}
		
//		public LambdaExpression Predicate {
//			get { return Data == null ? null : (LambdaExpression)Data.FromJsv<ExpressionNode>().ToExpression(); }
//			set { Data = Visitor.Visit(value).ToExpressionNode().ToJsv<ExpressionNode>(); }
//		}
//		
//		public LambdaExpression Predicate {
//			get
//			{
//				return Data == null ? null : (LambdaExpression)Data.FromJsv<ExpressionNode>().ToExpression();
//			}
//			set
//			{
//				IMongoQuery query;
//				Query< q;
//			}
//		}
//
//		public QueryRequest(LambdaExpression predicate, string collectionName)
//		{
//			this.Predicate = predicate;
//			this.CollectionName = collectionName;
//			ArtefactsClient.Log.Debug(this);
//			ArtefactsClient.LogWriter.WriteLine(this.FormatString());
//		}
		
		public QueryRequest()
		{
			;	
		}
		
		public QueryRequest(IMongoQuery query, string collectionName)
//			: base((QueryDocument)query)
		{
			CollectionName = collectionName;
			Query = (QueryDocument)query;
		}
		
//		public override string ToString()
//		{
//			return string.Format("[QueryRequest: CollectionName={0}, Predicate={1}, Data={2}]", CollectionName, null/*Predicate*/, Data);
//		}
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


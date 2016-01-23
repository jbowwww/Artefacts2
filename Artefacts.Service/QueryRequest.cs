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

namespace Artefacts.Service
{
	[DataContract]
	[Route("/Artefacts/{CollectionName}/{DataFormat}/{QueryData}/", "GET")]
	public class QueryRequest : IReturn<QueryResults>
	{
		#region Static members
		/// <summary>
		/// Gets or sets the visitor.
		/// </summary>
//		public static ExpressionVisitor Visitor { get; set; }
		
		public static ExpressionSerializer Serializer { get; set; }
		
		static QueryRequest()
		{
//			Visitor = new ClientQueryVisitor<Type>();
			Serializer = new ExpressionSerializer(new JsonSerializer());
		}

		public static QueryRequest Make<T>(Expression<Func<T, bool>> predicate)
		{
			return new QueryRequest(
				Artefact.MakeSafeCollectionName(typeof(T).FullName),
				Query<T>.Where((Expression<Func<T, bool>>) new ClientQueryVisitor<T>().Visit(predicate)));
		}

		public static QueryRequest Make<T>(string collectionName, Expression<Func<T, bool>> predicate)
		{
			return new QueryRequest(
				Artefact.MakeSafeCollectionName(collectionName),
				Query<T>.Where((Expression<Func<T, bool>>) new ClientQueryVisitor<T>().Visit(predicate)));
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
		
		[DataMember(Order = 3)]
		public string DataFormat {
			get;
			set;
		}
		
		public QueryDocument Query {
			get
			{
				return _query ??
					(_query = new QueryDocument(
						BsonDocument.Parse(
							QueryData/*.UrlDecode()*/)
					));
				
			}
			set
			{
				QueryData = (_query = value) == null ? 
					string.Empty
				:	_query.ToString()/*.UrlEncode()*/;
				DataFormat = "Query";
			}
		}
		private QueryDocument _query;
		
		public Expression Expression {
			get
			{
				if (_expression != null)
					return _expression;
				ExpressionNode node = QueryData/*.UrlDecode()*/.FromJson<ExpressionNode>();
				_expression = node.ToExpression();
//				_expression = Serializer.DeserializeText(QueryData.UrlDecode());
				return _expression;
				//Serializer.DeserializeText(QueryData));
			}
			set
			{
//				QueryData = ExpressionFormatter.ToString(value);
				ExpressionNode node = (_expression = value).ToExpressionNode();
				QueryData = ServiceStack.StringExtensions.ToJson<ExpressionNode>(node)/*r4.UrlEncode()*/;
//				QueryData = Serializer.SerializeText(_expression = value).UrlEncode();
				DataFormat = "Expression";
			}
		}
		private Expression _expression;
		#endregion

		public QueryRequest(string collectionName, IMongoQuery query)
		{
			if (collectionName.IsNullOrSpace())
				throw new ArgumentOutOfRangeException("collectionName", collectionName, "collectionName is NULL or whitespace");
			if (query == null)
				throw new ArgumentNullException("query");
			CollectionName = collectionName;
			Query = (QueryDocument)query;
		}
		
		public QueryRequest(string collectionName, Expression expression)
		{
			if (collectionName.IsNullOrSpace())
				throw new ArgumentOutOfRangeException("collectionName", collectionName, "collectionName is NULL or whitespace");
			if (expression == null)
				throw new ArgumentNullException("expression");
			CollectionName = collectionName;
			Expression = expression;
		}
		
		public override string ToString()
		{
			return string.Format("[QueryRequest: CollectionName={0} DataFormat={1} QueryData={2}]", CollectionName, DataFormat, QueryData);
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


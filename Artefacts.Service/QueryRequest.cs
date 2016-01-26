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
//	[Route("/Artefacts/{CollectionName}/{DataFormat}/{QueryType}/{QueryData}/", "GET")]
	[Route("/Artefacts/{CollectionName}/{DataFormat}/{QueryData}/", "GET")]
public class QueryRequest<T> : QueryRequest, IReturn<object>
	{
		#region Static members
		public static readonly ClientQueryVisitor<T> Visitor = new ClientQueryVisitor<T>();
		public static readonly ArtefactQueryTranslator<T> Translator = new ArtefactQueryTranslator<T>();
		#endregion
		
		public QueryRequest(string collectionName, Expression expression) : base(collectionName)
		{
			
		}
	}
	
	[DataContract]
	[Route("/Artefacts/{CollectionName}/{DataFormat}/{QueryType}/{QueryData}/", "GET")]
public class QueryRequest : IReturn<QueryResults>//<object>
	{
//		public static ExpressionSerializer Serializer { get; set; }	
//		static QueryRequest()
//		{
//			Visitor = new ClientQueryVisitor<Type>();
//			Serializer = new ExpressionSerializer(new JsonSerializer());
//		}

		public static QueryRequest Make<T>(Expression<Func<T, bool>> predicate)
		{
			return new QueryRequest(
				Artefact.MakeSafeCollectionName(typeof(T).FullName),
				Query<T>.Where((Expression<Func<T, bool>>) new ClientQueryVisitor<T>().Visit(predicate)));
		}

//		public static QueryRequest Make<T>(string collectionName, Expression<Func<T, bool>> predicate)
//		{
//			return new QueryRequest(
//				Artefact.MakeSafeCollectionName(collectionName),
//				Query<T>.Where((Expression<Func<T, bool>>) new ClientQueryVisitor<T>().Visit(predicate)));
//		}
		
		public static QueryRequest Make<T>(string collectionName, Expression expression)
		{
			ClientQueryVisitor<T> Visitor = new ClientQueryVisitor<T>();
			ArtefactQueryTranslator<T> Translator = new ArtefactQueryTranslator<T>();
			Expression visitedExpression = Visitor.Visit(expression);
			QueryDocument query = (QueryDocument)Translator.Translate(visitedExpression);
			string queryType = "std";
			MethodCallExpression mce = visitedExpression as MethodCallExpression;
			if (mce != null && mce.Method.DeclaringType == typeof(System.Linq.Queryable))	// && typeof enumerable??
				queryType = mce.Method.Name;
			return new QueryRequest(Artefact.MakeSafeCollectionName(collectionName), query, queryType);
		}

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
		public string QueryType {
			get;
			protected set;
		}
		
		[DataMember(Order = 4)]
		public string DataFormat {
			get;
			set;
		}
		
		[IgnoreDataMember]
		public QueryDocument Query {
			get
			{
				return _query ??
					(_query =
					 QueryData == "(null)" ? null:
						 new QueryDocument(
							BsonDocument.Parse(
								QueryData/*.UrlDecode()*/)
						));
				
			}
			set
			{
				QueryData = (_query = value) == null ? 
					"(null)"	//string.Empty
				:	_query.ToString()/*.UrlEncode()*/;
				DataFormat = "Query";
			}
		}
		private QueryDocument _query;
		
		[IgnoreDataMember]
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

		protected QueryRequest(string collectionName)
		{
			if (collectionName.IsNullOrSpace())
				throw new ArgumentOutOfRangeException("collectionName", collectionName, "collectionName is NULL or whitespace");
			CollectionName = collectionName;
		}
		public QueryRequest(string collectionName, IMongoQuery query, string queryType = "") : this(collectionName)
		{
			if (query == null)
				throw new ArgumentNullException("query");
			Query = (QueryDocument)query;
			QueryType = queryType;
		}
		
		public QueryRequest(string collectionName, Expression expression) : this(collectionName)
		{
//			if (collectionName.IsNullOrSpace())
//				throw new ArgumentOutOfRangeException("collectionName", collectionName, "collectionName is NULL or whitespace");
			if (expression == null)
				throw new ArgumentNullException("expression");
//			CollectionName = collectionName;
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


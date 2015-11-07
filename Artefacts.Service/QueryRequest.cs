using Artefacts;
using ServiceStack;
using System.Linq.Expressions;
using Serialize.Linq.Nodes;
using System.Runtime.Serialization;
using System;
using ServiceStack.Messaging;

namespace Artefacts.Service
{
	[DataContract, Route("/Query/{Data}", "GET")]
	public class QueryRequest : IReturn<QueryResults>
	{
		public static QueryRequest Make<T>(Expression<Func<T, bool>> predicate)
		{
			return new QueryRequest((LambdaExpression)predicate);
		}
		
		[DataMember(Order=1)]
		public string Data { get; set; }

		public LambdaExpression Predicate {
			get { return Data == null ? null : (LambdaExpression)Data.FromJsv<ExpressionNode>().ToExpression(); }
			set { Data = value.ToExpressionNode().ToJsv<ExpressionNode>(); }
		}
		
		public QueryRequest(LambdaExpression predicate)
		{
			this.Predicate = predicate;
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


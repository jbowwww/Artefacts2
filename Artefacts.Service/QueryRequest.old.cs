using Artefacts;
using ServiceStack;
using System.Linq.Expressions;
using Serialize.Linq.Nodes;
using System.Runtime.Serialization;
using System;
using ServiceStack.Messaging;

namespace Artefacts.Service
{
	public class QueryRequest<T> : QueryRequest
	{
		public static QueryRequest<T> Make(Expression<Func<T, bool>> predicate, ExpressionVisitor visitor = null)
		{
			return new QueryRequest<T>((Expression<Func<T, bool>>)(visitor ?? QueryRequest.DefaultVisitor).Visit(predicate));
		}
		
		public QueryRequest(Expression<Func<T, bool>> predicate) : base(predicate) { }
	}
	
	/// <summary>
	/// <see cref="MatchArtefactRequest"/> may become obsolete by replacig with this
	/// 	- Although this could have wider uses
	/// </summary>
	[DataContract]
	[Route("/Query/", "GET")]
	public class QueryRequest : IReturn<QueryResults>
	{
		protected static ExpressionVisitor DefaultVisitor {
			get { return _defaultVisitor; }
		}
		private static ExpressionVisitor _defaultVisitor = new ClientQueryVisitor();
		
		public static QueryRequest Make<T>(Expression<Func<T, bool>> predicate, ExpressionVisitor visitor = null)
		{
			return new QueryRequest<T>((Expression<Func<T, bool>>)(visitor ?? DefaultVisitor).Visit((Expression)predicate));
		}

		public static QueryRequest Make(Expression<Func<dynamic, bool>> predicate, ExpressionVisitor visitor = null)
		{
			return Make<dynamic>(predicate, visitor);
		}

		[DataMember(Order=1)]
		public string Data {
			get;
			set;
		}

		public LambdaExpression Where {
			get { return Data == null ? null : (LambdaExpression)Data.FromJsv<ExpressionNode>().ToExpression(); }
			set { Data = value.ToExpressionNode().ToJsv<ExpressionNode>(); }
		}

		public QueryRequest(Expression where)
		{
			if (where.NodeType != ExpressionType.Lambda)
				throw new ArgumentException("Not a LambdaExpression", "where");
			this.Where = (LambdaExpression)where;
		}
	}
}


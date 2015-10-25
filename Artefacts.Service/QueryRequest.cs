using Artefacts;
using ServiceStack;
using System.Linq.Expressions;
using Serialize.Linq.Nodes;
using System.Runtime.Serialization;
using System;

namespace Artefacts.Service
{
	public class QueryRequest<T> : QueryRequest
	{
		public QueryRequest(Expression<Func<T, bool>> where, ExpressionVisitor visitor = null)
		: base((Expression)where, visitor)
		{
		}
	}
	
	/// <summary>
	/// <see cref="MatchArtefactRequest"/> may become obsolete by replacig with this
	/// 	- Although this could have wider uses
	/// </summary>
	[DataContract]
	[Route("/Query/", "GET")]
	public class QueryRequest : IReturn<QueryResults>
	{
		static private ExpressionVisitor _expressionVisitor = new ClientQueryVisitor();

		private ExpressionVisitor _visitor = null;

		[DataMember(Order=1)]
		public string Data { get; set; }

		public LambdaExpression Where {
			get { return Data == null ? null : (LambdaExpression)Data.FromJson<ExpressionNode>().ToExpression(); }
			set { Data = _visitor.Visit(value).ToExpressionNode().ToJson<ExpressionNode>(); }
		}

		public QueryRequest(Expression where, ExpressionVisitor visitor = null)
		{
			this._visitor = visitor ?? _expressionVisitor ?? new ClientQueryVisitor();
			if (where.NodeType != ExpressionType.Lambda)
				throw new ArgumentException("Not a LambdaExpression", "where");
			this.Where = (LambdaExpression)where;
		}
		
		public static QueryRequest Make<T>(Expression<Func<T, bool>> where, ExpressionVisitor visitor = null)
		{
			return new QueryRequest((Expression)where, visitor);
		}
	}
}


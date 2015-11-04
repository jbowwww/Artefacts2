using Artefacts;
using ServiceStack;
using System.Linq.Expressions;
using Serialize.Linq.Nodes;
using System.Runtime.Serialization;
using System;
using ServiceStack.Messaging;

namespace Artefacts.Service
{
//	public class QueryRequest<T> : QueryRequest
//	{
//		public QueryRequest(Expression<Func<T, bool>> where, ExpressionVisitor visitor = null)
//		: base((Expression)where, visitor)
//		{
//		}
//	}
	
	/// <summary>
	/// <see cref="MatchArtefactRequest"/> may become obsolete by replacig with this
	/// 	- Although this could have wider uses
	/// </summary>
	[DataContract]
	[Route("/Query/", "GET")]
	public class QueryRequest : IReturn<QueryResults>
	{
		public static QueryRequest Make(Expression<Func<dynamic, bool>> where)
		{
			return new QueryRequest((Expression)where);
		}
		
		public static QueryRequest Make<T>(Expression<Func<T, bool>> where)
		{
			return new QueryRequest((Expression)where);
		}

		[DataMember(Order=1)]
		public string Data { get; set; }

		public LambdaExpression Where {
			get { return Data == null ? null : (LambdaExpression)Data.FromJson<ExpressionNode>().ToExpression(); }
			set { Data = value.ToExpressionNode().ToJson<ExpressionNode>(); }
		}

		public QueryRequest(Expression where)
		{
			if (where.NodeType != ExpressionType.Lambda)
				throw new ArgumentException("Not a LambdaExpression", "where");
			this.Where = (LambdaExpression)where;
		}
	}
}


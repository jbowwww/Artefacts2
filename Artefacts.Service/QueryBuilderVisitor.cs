using System;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace Artefacts.Service
{
	public class QueryBuilderVisitor : ExpressionVisitor
	{
		IMongoQuery _query;
		
		public QueryBuilderVisitor()
		{
			
		}
		
		public override System.Linq.Expressions.Expression Visit(System.Linq.Expressions.Expression exp)
		{
			return base.Visit(exp);
		}
		
		protected override System.Linq.Expressions.Expression VisitBinary(System.Linq.Expressions.BinaryExpression b)
		{
			IMongoQuery qLeft, qRight;
			Visit(b.Left);
			qLeft = _query;
			Visit(b.Right);
			qRight = _query;
			_query = b.NodeType == System.Linq.Expressions.ExpressionType.
			
			return base.VisitBinary(b);
		}
		
		
		protected override System.Linq.Expressions.Expression VisitLambda(System.Linq.Expressions.LambdaExpression lambda)
		{
//			Query.
			
			lambda.Body
			return base.VisitLambda(lambda);
		}
	}
}


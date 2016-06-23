using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Generic;
using MongoDB.Driver.Linq;

namespace Artefacts.Service.Extensions
{
	public static class Expressions_Ext
	{
		static readonly Type _enumerableStaticType = typeof(System.Linq.Enumerable);
		static readonly Type _queryableStaticType = typeof(System.Linq.Queryable);
		static readonly Type _enumerableType = typeof(System.Collections.IEnumerable);
		static readonly Type _queryableType = typeof(System.Linq.IQueryable);
		static readonly Type _queryResultType = typeof(QueryResults);
		static readonly Type _artefactType = typeof(Artefact);
		
		public static bool IsLinqMethod(this MethodInfo method) {
			ParameterInfo[] parameters = method.GetParameters();
			return parameters.Length >= 1 && _enumerableType.IsAssignableFrom(parameters[0].ParameterType) &&
				(method.DeclaringType.Equals(_queryableStaticType) || method.DeclaringType.Equals(_enumerableStaticType));
		}
		
		public static bool IsWhereClause(this Expression expression) {
			MethodCallExpression mce = expression as MethodCallExpression;
			return mce != null && mce.Method.IsLinqMethod() && mce.Method.Name == "Where";
		}
		
		public static Expression CombineConsecutiveWhereClauses(this Expression expression) {
			if (expression.IsWhereClause())
			{
				MethodCallExpression mce = (MethodCallExpression)expression;
				LambdaExpression lambda  = (LambdaExpression)((UnaryExpression)((MethodCallExpression)expression).Arguments[1]).Operand;
				ParameterExpression parameter = lambda.Parameters[0];
				Expression lambdaBody = lambda.Body;
				Expression arg0 = expression;
				while ((arg0 = ((MethodCallExpression)arg0).Arguments[0]).IsWhereClause())
				{
					LambdaExpression innerLambda = (LambdaExpression)((UnaryExpression)((MethodCallExpression)arg0).Arguments[1]).Operand;
					Expression innerBody = ExpressionParameterReplacer.ReplaceParameter(innerLambda.Body, innerLambda.Parameters[0], parameter);
					lambdaBody = Expression.And(innerBody, lambdaBody);
				}
				return Expression.Call(mce.Method.DeclaringType, mce.Method.Name, mce.Method.GetGenericArguments(), arg0, Expression.Lambda(lambdaBody, parameter));
			}
			return expression;
		}
	}
}


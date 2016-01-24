using System;
using MongoDB.Driver.Linq;
using MongoDB.Driver;
using System.Linq.Expressions;
using MongoDB.Driver.Builders;
using System.Collections.Generic;
using System.Linq;

namespace Artefacts.Service
{
	public class ArtefactQueryTranslator<T>
	{
		#region Constants
		public readonly Type ElementType = typeof(T);
		public readonly Type EnumerableType = typeof(IEnumerable<T>);
		#endregion
		
		public IMongoQuery Translate(Expression expression)
		{
			if (expression.NodeType == ExpressionType.Constant && typeof(ArtefactQueryable<T>).IsAssignableFrom(expression.Type))
			{
				return Query.Null;
			}
			else if (expression.NodeType == ExpressionType.Call)
			{
				return TranslateMethodCall((MethodCallExpression)expression);
			}
			else
			{
				throw new ArgumentOutOfRangeException("expression", expression, "Expression of type \"" + expression.NodeType + "\" not supported");
			}
		}
		
		protected IMongoQuery TranslateMethodCall(MethodCallExpression mce)
		{
			if (mce.Method.IsStatic)
			{
				ConstantExpression mceObj = mce.Object as ConstantExpression;
				if (mceObj == null)
				{
					// Looks like a LINQ or LINQ-style method
					if (mce.Arguments.Count > 0 && EnumerableType.IsAssignableFrom(mce.Arguments[0].Type))
					{
						switch (mce.Method.Name)
						{
							case "Where":
								if (mce.Arguments.Count != 2 || typeof(System.Func<T, bool>).IsAssignableFrom(mce.Arguments[1].Type))
									throw new ArgumentOutOfRangeException("mce.Arguments", mce.Arguments, "Where method has incorrect number or type of arguments");
								IMongoQuery q = Translate(mce.Arguments[0]);
								IMongoQuery q2 = Query<T>.Where((Expression<Func<T, bool>>)StripQuotes(mce.Arguments[1]));
								if (q != null)
									return Query.And(q, q2);
								return q2;

							case "Select":
								if (mce.Arguments.Count != 2 ||
									!typeof(System.Func<,>).MakeGenericType(
									ElementType,
									mce.Method.ReturnType.GetGenericArguments()[0])
								   	.IsAssignableFrom(mce.Arguments[1].Type))
									throw new ArgumentOutOfRangeException("mce.Arguments", mce.Arguments, "Select method has incorrect number or type of arguments");
//								IMongoQuery q0 = Translate(mce.Arguments[0]);
//								IMongoQuery q1 = new SelectQuery()
							
//								new List<object>().AsQueryable().Select(o => o.GetType());
								throw new NotImplementedException();
								break;
								
									
							// 2 versions of count, one with a predicate one without
//							case "Count":
//								if (mce.Arguments.Count == 1)
//								{
//									IMongoQuery qInner = Translate(mce.Arguments[0]);
//								}
								
							// TODO: Support for a fuckload of these functions. Cunty fuckin MOngo has all this translation implemented but only
							// if it is an expression for specifically a MongoQueryable<> rather than IQueryable<>
							default:
								throw new NotSupportedException();
						}
					}
				}
			}
			return Query.Null;
		}
		
		protected static Expression StripQuotes(Expression e)
		{
			while (e != null && e.NodeType == ExpressionType.Quote)
				e = ((UnaryExpression)e).Operand;
			return e;
		}
	}
}
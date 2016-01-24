using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.ObjectModel;
using MongoDB.Bson;

namespace Artefacts.Service
{
//	public class ClientQueryVisitor : ClientQueryVisitor<Artefact>
//	{
//	}
//
//	public class ClientQueryVisitor<TArtefact> : ExpressionVisitor where TArtefact : Artefact
	
	public class ClientQueryVisitor<T> : ExpressionVisitor
	{
		static readonly Type _enumerableStaticType = typeof(System.Linq.Enumerable);
		static readonly Type _queryableStaticType = typeof(System.Linq.Queryable);
		static readonly Type _queryResultType = typeof(QueryResults);
		static readonly Type _artefactType = typeof(Artefact);
		readonly Type _queryableType = typeof(IQueryable<T>);
		readonly Type _enumerableType = typeof(IEnumerable<T>);
		readonly Type _elementType = typeof(T);
		
		const BindingFlags bf =
			BindingFlags.GetField | BindingFlags.GetProperty |
			BindingFlags.Instance | BindingFlags.Static |
			BindingFlags.Public | BindingFlags.NonPublic;
		
//		protected override Expression VisitConstant(ConstantExpression c)
//		{
//			return Expression.Convert(c, c.Type);
//			return base.VisitConstant(c);
//		}
		
//		protected override Expression VisitBinary(BinaryExpression b)
//		{
//			return Expression.MakeBinary(b.NodeType, Expression.TypeAs(b.Left, b.Left.Type), Expression.TypeAs(b.Right, b.Right.Type));
//		}
		
//		protected override Expression VisitParameter(ParameterExpression p)
//		{
//			if (p.Name == "collection" &&
//			 (	p.Type.GetInterfaces().Contains(typeof(IQueryable))
//			 || p.Type.GetInterfaces().Contains(typeof(IEnumerable<T>)) ) )
//				return Expression.Parameter(typeof(IQueryable<Artefact>), "collection");
//			else
//			if (p.Type == typeof(T))
//				return Expression.Parameter(typeof(Artefact), p.Name);
//			return p;
//		}
		
		protected override Expression VisitMemberAccess(MemberExpression m)
		{
//			if (m.Expression.NodeType == ExpressionType.Parameter)
//			{
//				ParameterExpression pe = m.Expression as ParameterExpression;
//			}
//			
//			if (m.Expression.NodeType == ExpressionType.RuntimeVariables)
//			{
//				RuntimeVariablesExpression rve = m.Expression as RuntimeVariablesExpression;
//				;
//			}
			
			Expression mExpression = Visit(m.Expression);
			if (mExpression != null)
			{
				if (mExpression.NodeType == ExpressionType.Parameter)
				{
					ParameterExpression pe = mExpression as ParameterExpression;
					if (pe.Type == typeof(Artefact))
					{
//						return Expression.Convert(
//							Expression.Property(pe,
//			                     typeof(BsonDocument).GetProperty("Item", typeof(BsonValue), new Type[] { typeof(string) }),
//			                     Expression.Constant(m.Member.Name)), m.Member.GetMemberReturnType());
						string bsonValueMemberName =
							m.Type == typeof(string) ? "AsString" :
							m.Type == typeof(Int64) ? "AsInt64" :
							m.Type == typeof(UInt64) ? "AsUInt64" :
							m.Type == typeof(Int32) ? "AsInt32" :
							m.Type == typeof(UInt32) ? "AsUInt32" :
							m.Type == typeof(bool) ? "AsBool" :
							m.Type == typeof(int) ? "AsInt64" :
							m.Type == typeof(DateTime) ? "AsDateTime" :
							m.Type == typeof(double) ? "AsDouble" :
								"RawValue";
						
//						return
//							Expression.Convert(
//								Expression.Property(
//									Expression.Parameter(typeof(DynamicObject), pe.Name),
//								m.Member.Name),
//								m.Type);
//							Expression.Convert(
//								Expression.Property(pe, "Item", new Expression[] { Expression.Constant(m.Member.Name) }),
//							m.Type);
						return Expression.Convert(
							Expression.Call(pe, typeof(Artefact).GetMethod("GetDataMember", new Type[] { typeof(string) }),
								new Expression[] { Expression.Constant(m.Member.Name) }),
							m.Type);
//							typeof(BsonValue).GetProperty(bsonValueMemberName, m.Type));
						
//						return Expression.Property(
//							Expression.Call(pe, typeof(BsonDocument).GetMethod("GetValue", new Type[] { typeof(string) }),
//								new Expression[] { Expression.Constant(m.Member.Name) }),
//							typeof(BsonValue).GetProperty(bsonValueMemberName, m.Type));
//						//, m.Member.GetMemberReturnType());
					}
				}
				
				if (mExpression.NodeType == ExpressionType.RuntimeVariables)
				{
					RuntimeVariablesExpression rve = mExpression as RuntimeVariablesExpression;
					;
				}
				
				// If is a member of a constant whose type is autoclass, invoke the member and return as a cosntant
				// (I *think* this replaces local variables references)
				if (mExpression.Type.IsAutoLayout && (mExpression.NodeType == ExpressionType.Constant))// && ((ConstantExpression)mExpression).Value != null))
				{
					return Expression.Constant(
						m.Member.DeclaringType.InvokeMember(
						m.Member.Name, bf, null,
						(mExpression as ConstantExpression).Value,
						new object[] { }), m.Type);
				}
				
				// If is a member of Artefact which doesn't actually exist in type, it must be a dynamic property. Convert
				// the member expression to a indexer expression to return the dynamic property (ie artefact[m.Member.Name])
//				else if (mExpression.Type == typeof(Artefact) && !typeof(Artefact).GetMembers(bf).Select(mi => mi.Name).Contains(m.Member.Name))
//				{
//					return Expression.Convert(
//						Expression.Call(mExpression, "GetDataMember", new Type[] {}, Expression.Constant(m.Member.Name)),
//						m.Member.GetMemberReturnType());
//				}
			}
			
			// default
			return base.VisitMemberAccess(m);
		}

		/// <summary>
		/// Visits the method call.
		/// </summary>
		/// <returns>The method call.</returns>
		/// <param name="m">M.</param>
		/// <remarks>
		/// // This only doesn't work because my queyr provider executes it using a repository query method (QueryExecute)
		/// that has return type of object, and should only be used for scalar results. (It does not have any artefact KnownType's)
		/// for method calls like FirstOrDefault() that produce an Artefact, you
		/// will need to find some way of detecting that return value, and running the method call expression's argument[0] expression as a query, to
		/// get artefact id, then retrieve artefact using repository getbyid() ??
		/// </remarks>
		protected override Expression VisitMethodCall(MethodCallExpression m)
		{
			Expression mObject = Visit(m.Object);
			IEnumerable<Expression> mArguments = VisitExpressionList(m.Arguments);			
			MethodInfo mi = m.Method;
			ParameterInfo[] pi = mi.GetParameters();
			
			if (mArguments != m.Arguments || mObject != m.Object)
			{

				// If method call is on a constant instance and all arguments are constants too,
				// invoke method and replace with constant expression of method's return value
				if (mObject != null && mObject.NodeType == ExpressionType.Constant
				 && mArguments.All<Expression>((arg) => arg.NodeType == ExpressionType.Constant))
					return Expression.Constant(m.Method.Invoke((mObject as ConstantExpression).Value,
						mArguments.Cast<ConstantExpression>().Select<ConstantExpression, object>((ce) => ce.Value).ToArray()));
	
				// LINQ queries are always extension methods so are in fact static
				if (mObject == null && mArguments.Count() > 0 && _enumerableType.IsAssignableFrom(mArguments.ElementAt(0).Type))
				{
					// Replace the predicate version of count() with a Where(predicate).Count
					if (mArguments.Count() == 2 && m.Method.Name == "Count" &&
						(m.Method.DeclaringType == _enumerableStaticType ||
					 	m.Method.DeclaringType == _queryableStaticType))
					{
						Expression innerWhere = Expression.Call(
							_enumerableStaticType, "Where",
							new Type[] {
								_enumerableType,//mArguments.ElementAt(0).Type,
								mArguments.ElementAt(1).Type 
							},
							mArguments.ElementAt(0), mArguments.ElementAt(1)
						);
						
						Expression outerCount = Expression.Call(
							_enumerableStaticType, "Count",
							new Type[] { 
								_enumerableType,//mArguments.ElementAt(0).Type },
							},
							innerWhere
						);
//							_enumerableType.GetMethod("Count", new Type[] { _enumerableType, typeof(Func<T,bool>) }),
					}
				}
				
				// If is a member of Artefact which doesn't actually exist in type, it must be a dynamic property. Convert
				// the member expression to a indexer expression to return the dynamic property (ie artefact[m.Member.Name])
//				else if (//mObject.Type == typeof(IEnumerable<Artefact>)
//				         mArguments.Count() > 0 && typeof(IQueryable<Artefact>).IsAssignableFrom(mArguments.ElementAt(0).Type)
//				 && 	(m.Method.DeclaringType == _enumerableStaticType
//				      || m.Method.DeclaringType == _queryableStaticType))
//				{
//					if (m.Method.IsGenericMethod)
//					{
//						Type[] genericArgs = m.Method.GetGenericArguments();
//						for (int i = 0; i < genericArgs.Length; i++)
//							if (genericArgs[i] == _elementType)
//								genericArgs[i] = typeof(Artefact);	//_artefactType;
//						MethodInfo mInfo = m.Method.GetGenericMethodDefinition().MakeGenericMethod(genericArgs);
//						return Expression.Call( mObject, mInfo, mArguments);
//					}
//					return Expression.Call(mObject, m.Method, mArguments);
//				}
//			
//				if (mObject == null || (mObject.NodeType == ExpressionType.Constant && mObject.Type.IsSpecialName))
//				{
//					return Expression.Convert(Expression.Constant(
//						m.Method.DeclaringType.InvokeMember(
//						m.Method.Name,
//						BindingFlags.Public | BindingFlags.NonPublic
//						| BindingFlags.InvokeMethod
//						| (mObject == null ? BindingFlags.Static : BindingFlags.Instance),
//						null,
//						mObject == null ? null : ((ConstantExpression)mObject).Value,
//						new object[] { }),
//						m.Type), m.Type);
//				}
				
				return Expression.Call(mObject, m.Method, mArguments);
			}
			
			// default
			return base.VisitMethodCall(m);
		}
		
		
		protected override Expression VisitNewArray(NewArrayExpression na)
		{
			// If array creation contains only constant elements, create the array instance, return a constant expression
			ReadOnlyCollection<Expression> naExpressions = VisitExpressionList(na.Expressions);
			if (naExpressions.All<Expression>((arg) => arg.NodeType == ExpressionType.Constant))
			{
				Array elements = Array.CreateInstance(na.Type.GetElementType(), naExpressions.Count);
				for (int i = 0; i < naExpressions.Count; i++)
					elements.SetValue(((ConstantExpression)naExpressions[i]).Value, i);
				return Expression.Constant(elements, na.Type);
			}
			
			// default
			return base.VisitNewArray(na);
		}
		
//		protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
//		{
//			return new ReadOnlyCollection<Expression>(original.Select(e => e.Type == _elementType ? _artefactType : e.Type).ToList());
//		}
		protected override Expression VisitLambda(LambdaExpression lambda)
		{
			return Expression.Lambda(Visit(lambda.Body), VisitExpressionList(lambda.Parameters));
		}
	}
}

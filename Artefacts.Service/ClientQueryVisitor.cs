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
		private bool _innerQuery = false;
		private ArtefactQueryable<T> _innerQueryable;

		public int InnerQueriesExecuted {
			get;
			protected set;
		}

		protected override Expression VisitConstant(ConstantExpression c)
		{
			if (typeof(ArtefactQueryable<T>).IsAssignableFrom(c.Type) && CurrentDepth > 1)
			{
				_innerQuery = true;
				_innerQueryable = (ArtefactQueryable<T>)c.Value;
				return c;
			}
			return base.VisitConstant(c);
		}

		protected override Expression VisitBinary(BinaryExpression b)
		{
			return Expression.MakeBinary(b.NodeType, Visit(b.Left), Visit(b.Right));
		}

		protected override Expression VisitUnary(UnaryExpression u)
		{
			return Expression.MakeUnary(u.NodeType, Visit(u.Operand), u.Type, u.Method);
		}
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
			Expression mExpression = Visit(m.Expression);
			if (mExpression != null)
			{
				if (mExpression.NodeType == ExpressionType.Parameter)
				{
					ParameterExpression pe = mExpression as ParameterExpression;
					if (pe.Type == typeof(Artefact))
					{
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
						return Expression.Convert(
							Expression.Call(pe, typeof(Artefact).GetMethod("GetDataMember", new Type[] { typeof(string) }),
						                 new Expression[] { Expression.Constant(m.Member.Name) }),
							m.Type);
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
					if (m.Method.DeclaringType == _enumerableStaticType || m.Method.DeclaringType == _queryableStaticType)
					{
						if (mArguments.Count() == 2)
						{
							if (m.Method.Name == "Count")
							{
								Expression innerWhere = Expression.Call(
									m.Method.DeclaringType, "Where",
									new Type[] { _elementType },
									Visit(mArguments.ElementAt(0)),
									Visit(mArguments.ElementAt(1)));
								Expression outerCount = Expression.Call(
//									m.Method.DeclaringType,
									_enumerableStaticType, "Count",
									new Type[] { _elementType },
								Expression.Convert(innerWhere, _enumerableType));
								return outerCount;
							}
							else if (m.Method.Name == "Where")
							{
								Expression where = Expression.Call(
									m.Method.DeclaringType, "Where",
									new Type[] { _elementType },
									Visit(mArguments.ElementAt(0)),
									Visit(mArguments.ElementAt(1)));
								return where;
							}
						}
						else if (mArguments.Count() == 1)
						{
							if (m.Method.Name == "Count")
							{
								return Expression.Call(
									m.Method.DeclaringType, "Count",
									new Type[] { _elementType },
									Visit(mArguments.ElementAt(0))
								);
							}
						}
					}
					throw new NotSupportedException(string.Format("Unsupported method \"", m.Method.DeclaringType.FullName, ".", m.Method.Name, "\""));
				}
			}
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

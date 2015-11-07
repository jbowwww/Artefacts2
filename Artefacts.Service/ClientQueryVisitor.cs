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
	
	public class ClientQueryVisitor : ExpressionVisitor
	{
		public static ClientQueryVisitor Singleton {
			get;
			private set;
		}
		
		static ClientQueryVisitor()
		{
			if (Singleton != null)
				throw new InvalidProgramException("ClientQueryVisitor.Singleton is not null in static constructor");
			Singleton = new ClientQueryVisitor();
		}
		
		const BindingFlags bf =
			BindingFlags.GetField | BindingFlags.GetProperty |
			BindingFlags.Instance | BindingFlags.Static |
			BindingFlags.Public | BindingFlags.NonPublic;

		protected override Expression VisitMemberAccess(MemberExpression m)
		{
			Expression mExpression = Visit(m.Expression);
			if (mExpression != null)
			{
				// If is a member of a constant whose type is autoclass, invoke the member and return as a cosntant
				// (I *think* this replaces local variables references)
				if (mExpression.Type.IsAutoClass || (mExpression.NodeType == ExpressionType.Constant && ((ConstantExpression)mExpression).Value != null))
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
//					return Expression.MakeIndex(
//						mExpression,
//						typeof(Artefact).GetProperty("Item", typeof(object), new Type[] { typeof(string) }),
//						new Expression[] { Expression.Constant(m.Member.Name) });
//				}
				
				// Gets unknown parameter exception - must have to change the local var definition in the lambda as well I guess (too hard? other/better ways?)
//				else if (mExpression.NodeType == ExpressionType.Parameter)
//				{
//					return Expression.Property(
////						.MakeMemberAccess(
//						Expression.Parameter(typeof(Artefact)), m.Member.Name);
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
			ReadOnlyCollection<Expression> mArguments = VisitExpressionList(m.Arguments);			
			MethodInfo mi = m.Method;
			ParameterInfo[] pi = mi.GetParameters();
			
			// If method call is on a constant instance and all arguments are constants too,
			// invoke method and replace with constant expression of method's return value
			if (mObject != null && mObject.NodeType == ExpressionType.Constant
			 && mArguments.All<Expression>((arg) => arg.NodeType == ExpressionType.Constant))
				return Expression.Constant(m.Method.Invoke((mObject as ConstantExpression).Value,
					mArguments.Cast<ConstantExpression>().Select<ConstantExpression, object>((ce) => ce.Value).ToArray()));

//			else if (pi.Length > 0 && (typeof(IEnumerable).IsAssignableFrom(pi[0].GetType()) || typeof(IQueryable).IsAssignableFrom(pi[0].GetType()))
//				&& mi.ReturnType.GetElementType() == null)
//			{
//				object id = Repository.QueryPreload(Visit(m.Arguments[0]).ToBinary());
//				int[] result = null;
//				if (mi.Name.Equals("First") || mi.Name.Equals("FirstOrDefault")
//				 || mi.Name.Equals("Single") || mi.Name.Equals("SingleOrDefault"))
//					result = Repository.QueryResults(id, 0, 1);
//				else if (mi.Name.Equals("Last") || mi.Name.Equals("LastOrDefault"))
//					result = Repository.QueryResults(id, m.Arguments.Count() - 1, 1);
//
//				// TODO: ElementAt()
//				Artefact artefact =
//					result != null && result.Length > 0 ?
//						Repository.GetById(result[0]) :
//						(Artefact)Activator.CreateInstance(m.Arguments[0].Type.GetElementType());
//				return Expression.Constant(artefact);
//			}
			
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
	}
}

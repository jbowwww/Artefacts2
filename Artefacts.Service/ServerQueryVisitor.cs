using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson;

namespace Artefacts.Service
{
	public class ServerQueryVisitor : ExpressionVisitor
	{
		const BindingFlags bf =
			BindingFlags.GetField | BindingFlags.GetProperty |
				BindingFlags.Instance | BindingFlags.Static |
				BindingFlags.Public | BindingFlags.NonPublic;

		public IQueryable<Artefact> Collection { get; set; }
		
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
			if (mObject != null && mObject.Type != typeof(BsonDocument) && mArguments.Count == 1)
				return Expression.MakeIndex(
					Expression.Parameter(typeof(BsonDocument), ((ParameterExpression)mObject).Name),
					typeof(BsonDocument).GetProperty("Item", new Type[] { typeof(string) }),
					mArguments);
			
			// default
			return base.VisitMethodCall(m);
		}
		
		protected override Expression VisitParameter(ParameterExpression p)
		{
			if (p.Name == "collection" && p.Type.GetGenericTypeDefinition() == typeof(IQueryable<>))
				return Expression.Constant(Collection);
			return base.VisitParameter(p);
		}
	}
}

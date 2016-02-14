using System;
using System.Text;
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
		public readonly Type EnumerableStaticType = typeof(System.Linq.Enumerable);
		public readonly Type QueryableStaticType = typeof(System.Linq.Queryable);
		#endregion
		
		private StringBuilder _serializedData = new StringBuilder(32);
		public readonly ClientQueryVisitor<T> Visitor = new ClientQueryVisitor<T>();
		
		public string SerializedData {
			get { return _serializedData.ToString(); }
		}

		public string LastOperation { get; protected set; }
		
		protected Expression StripQuotes(UnaryExpression e)
		{
			while (e != null && e.NodeType == ExpressionType.Quote)
				return Visitor.Visit(e.Operand);
			return e;
		}

		public Expression Visit(Expression expression)
		{
			Expression visitedExpression = Visitor.Visit(expression);
			return visitedExpression;
		}
		
		public IMongoQuery Translate(Expression e)
		{
			Expression ve = Visit(e);
//			if (typeof(ArtefactQueryable<T>).IsAssignableFrom(ve.Type))
//			{
				if (ve.NodeType == ExpressionType.Constant)
				{
					return Query.Null;
				}
				ParameterExpression pe = ve as ParameterExpression;
				if (pe != null)
				{
				LastOperation = "Where";
					_serializedData.Append(pe.Name);
					return Query.Null;
				}
//			}
			
//			if (ve.NodeType == ExpressionType.Quote)
//				return Translate(StripQuotes(ve));
			else if (ve.NodeType == ExpressionType.UnaryPlus)
				return TranslateUnary((UnaryExpression)ve);
			else if (ve.NodeType == ExpressionType.Convert)
				return TranslateConvert((UnaryExpression)ve);
			BinaryExpression be = ve as BinaryExpression;
			if (be != null)
				return TranslateBinary((BinaryExpression)ve);
			
			
			MemberExpression me = ve as MemberExpression;
			if (me != null)
				return TranslateMember(me);
			
			if (ve.NodeType == ExpressionType.Call)
				return TranslateMethodCall((MethodCallExpression)ve);
			else if (ve.NodeType == ExpressionType.Lambda)
				return TranslateLambda((LambdaExpression)ve);
			throw new ArgumentOutOfRangeException("visitedExpression", ve, "Expression of type \"" + ve.NodeType + "\" not supported");
		}
		
		protected IMongoQuery TranslateConvert(UnaryExpression ue)
		{
			_serializedData.AppendFormat("({0})(", ue.Type.FullName);
			Translate(ue.Operand);
			_serializedData.Append(")");
			return null;
		}
		
		protected IMongoQuery TranslateUnary(UnaryExpression ue)
		{
			if (ue.Method != null)
				_serializedData.Append(ue.Method.Name + "(");
			Translate(ue.Operand);
			if (ue.Method != null)
				_serializedData.Append(")");
			return null;
		}
		
		protected IMongoQuery TranslateBinary(BinaryExpression be)
		{
			Translate(be.Left);
			if (be.Method != null)
				_serializedData.Append(be.Method.Name);
			switch (be.NodeType)
			{
				case ExpressionType.Equal: _serializedData.Append("=="); break;
				case ExpressionType.NotEqual: _serializedData.Append("!="); break;
				case ExpressionType.LessThan: _serializedData.Append("<"); break;
				case ExpressionType.LessThanOrEqual: _serializedData.Append("<="); break;
				case ExpressionType.GreaterThan: _serializedData.Append(">"); break;
				case ExpressionType.GreaterThanOrEqual: _serializedData.Append(">="); break;
				case ExpressionType.Add: _serializedData.Append("+"); break;
				case ExpressionType.Subtract: _serializedData.Append("-"); break;
				case ExpressionType.Multiply: _serializedData.Append("*"); break;
				case ExpressionType.Divide: _serializedData.Append("/"); break;
				case ExpressionType.Or: _serializedData.Append("|"); break;
				case ExpressionType.OrElse: _serializedData.Append("||"); break;
				case ExpressionType.And: _serializedData.Append("&"); break;
				case ExpressionType.AndAlso: _serializedData.Append("&&"); break;
				default: throw new InvalidOperationException("Unknown BinaryExpression NodeType: " + be.NodeType);
			}
			Translate(be.Right);
			return null;
		}
		
		protected IMongoQuery TranslateMember(MemberExpression me)
		{
			Translate(me.Expression);
			_serializedData.AppendFormat(".{0}", me.Member.Name);
			return null;
		}
		
		protected IMongoQuery TranslateMethodCall(MethodCallExpression mce)
		{
			if (mce.Method.IsStatic)
			{
				ConstantExpression mceObj = mce.Object as ConstantExpression;
				if (mceObj == null)
				{
					// Looks like a LINQ or LINQ-style method
					if ( mce.Arguments.Count > 0 && EnumerableType.IsAssignableFrom(mce.Arguments[0].Type) &&
					    (QueryableStaticType.Equals(mce.Method.DeclaringType) || EnumerableStaticType.Equals(mce.Method.DeclaringType)))
					{
						IMongoQuery q = Translate(mce.Arguments[0]);
						_serializedData.Append('.');
						_serializedData.Append(mce.Method.Name);
						LastOperation = mce.Method.Name;
						if (mce.Arguments.Count == 1)
						{
							_serializedData.Append("()");
//							if (mce.Method.Name == "Count")
//								;
						}	
						else if (mce.Arguments.Count >= 2)		// ||*/ typeof(System.Func<T, bool>).IsAssignableFrom(mce.Arguments[1].Type))
						{
							_serializedData.Append('(');
							for (int i = 1; i < mce.Arguments.Count; i++)
							{
								if (i > 1)
									_serializedData.Append(", ");
								Translate(mce.Arguments[i]);
//								Expression ve = Visit(StripQuotes(mce.Arguments[i]));
//								qp = Query<T>.Where((Expression<Func<T, bool>>)ve);		//Translate();
//								qp = Query.And(qp);
							}
							_serializedData.Append(')');							
							IMongoQuery q2 = Query.Null;
							if (mce.Method.Name == "Where")
								q2 = Query<T>.Where((Expression<Func<T, bool>>)StripQuotes((UnaryExpression)mce.Arguments[1]));
							if (q == null)
								return q2;
							else if (q2 != null)
								return Query.And(q, q2);
							return q;
						}
						return q;
					}
				}
			}
			return Query.Null;
		}
		
		protected IMongoQuery TranslateLambda(LambdaExpression lambda)
		{
			_serializedData.Append("(");
			for (int i = 0; i < lambda.Parameters.Count; i++)
			{
				if (i > 0)
					_serializedData.Append(", ");
				Translate(lambda.Parameters[i]);
			}
			_serializedData.Append(") => ");
			Translate(lambda.Body);
			return null;
		}
	}
}
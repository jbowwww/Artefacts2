using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

namespace Artefacts
{
	/// <summary>
	/// Expression visitor.
	/// </summary>
	/// <exception cref='Exception'>
	/// Represents errors that occur during application execution.
	/// </exception>
	/// <remarks>
	/// TODO: I think you can get rid of this class, as it's actually in System.Linq.Expressions. I found the code on the net &
	/// modified it slightly but it looks virtually identical. Derive ClientExpressionVisitor(and others) same as you have already.
	/// </remarks>
	public abstract class ExpressionVisitor
	{
//		internal class VisitedConstantExpression
//		 : Expression
//		{
//			public object Value { get; set; }
//			
//			public bool Visited { get; set; }
//			
//			public VisitedConstantExpression(object value, Type type, bool visited = true)
//			 : base(ExpressionType.Constant, type)
//			{
//				Value = value;
//			}
//		}
	
		protected static Expression StripQuotes(Expression e)
		{
			while (e != null && e.NodeType == ExpressionType.Quote)
				e = ((UnaryExpression)e).Operand;
			return e;
		}

		protected Expression _previousVisit, _thisVisit = null;
		protected HashSet<Expression> _visited = new HashSet<Expression>();
		
		protected bool Visited(Expression expression)
		{
			if (_visited.Contains(expression))
				return true;
			_visited.Add(expression);
			return false;
		}
		
		public virtual Expression Visit(Expression exp)
		{
			_previousVisit = _thisVisit;
			_thisVisit = exp;
			if (exp == null)
				return exp;
//			if (Visited(exp))
//				return exp;
			switch (exp.NodeType)
			{
				case ExpressionType.Quote:
				return this.Visit(StripQuotes(exp));
			case ExpressionType.Negate:
			case ExpressionType.NegateChecked:
			case ExpressionType.Not:
			case ExpressionType.Convert:
			case ExpressionType.ConvertChecked:
			case ExpressionType.ArrayLength:
			case ExpressionType.TypeAs:
				return this.VisitUnary((UnaryExpression)exp);
			case ExpressionType.Add:
			case ExpressionType.AddChecked:
			case ExpressionType.Subtract:
			case ExpressionType.SubtractChecked:
			case ExpressionType.Multiply:
			case ExpressionType.MultiplyChecked:
			case ExpressionType.Divide:
			case ExpressionType.Modulo:
			case ExpressionType.And:
			case ExpressionType.AndAlso:
			case ExpressionType.Or:
			case ExpressionType.OrElse:
			case ExpressionType.LessThan:
			case ExpressionType.LessThanOrEqual:
			case ExpressionType.GreaterThan:
			case ExpressionType.GreaterThanOrEqual:
			case ExpressionType.Equal:
			case ExpressionType.NotEqual:
			case ExpressionType.Coalesce:
			case ExpressionType.ArrayIndex:
			case ExpressionType.RightShift:
			case ExpressionType.LeftShift:
			case ExpressionType.ExclusiveOr:
				return this.VisitBinary((BinaryExpression)exp);
			case ExpressionType.TypeIs:
				return this.VisitTypeIs((TypeBinaryExpression)exp);
			case ExpressionType.Conditional:
				return this.VisitConditional((ConditionalExpression)exp);
			case ExpressionType.Constant:
				return this.VisitConstant((ConstantExpression)exp);
			case ExpressionType.Parameter:
				return this.VisitParameter((ParameterExpression)exp);
			case ExpressionType.MemberAccess:
				return this.VisitMemberAccess((MemberExpression)exp);
			case ExpressionType.Call:
				return this.VisitMethodCall((MethodCallExpression)exp);
			case ExpressionType.Lambda:
				return this.VisitLambda((LambdaExpression)exp);
			case ExpressionType.New:
				return this.VisitNew((NewExpression)exp);
			case ExpressionType.NewArrayInit:
			case ExpressionType.NewArrayBounds:
				return this.VisitNewArray((NewArrayExpression)exp);
			case ExpressionType.Invoke:
				return this.VisitInvocation((InvocationExpression)exp);
			case ExpressionType.MemberInit:
				return this.VisitMemberInit((MemberInitExpression)exp);
			case ExpressionType.ListInit:
				return this.VisitListInit((ListInitExpression)exp);
			case ExpressionType.Index:
				return exp;
			default:
				throw new Exception(string.Format("Unhandled expression type: '{0}'", exp.NodeType));
			}
		}
		
		#region Expression visitation methods
		protected virtual Expression VisitUnary(UnaryExpression u)
		{
//			if (Visited(u))
//				return u;
			Expression operand = this.Visit(u.Operand);
			if (operand != u.Operand)
				return Expression.MakeUnary(u.NodeType, operand, u.Type, u.Method);
			return u;
		}
 
		protected virtual Expression VisitBinary(BinaryExpression b)
		{
//			if (Visited(b))
//				return b;
//			using (IDisposable vmsu = new VisitationMethodStackUpdater(VisitStack, b))
//			{
//				_sb.Append(b.ToString());
				Expression left = this.Visit(b.Left);
				Expression right = this.Visit(b.Right);
				Expression conversion = this.Visit(b.Conversion);
				if (left != b.Left || right != b.Right || conversion != b.Conversion)
				{
					if (b.NodeType == ExpressionType.Coalesce && b.Conversion != null)
						return Expression.Coalesce(left, right, conversion as LambdaExpression);
					else
						return Expression.MakeBinary(b.NodeType, left, right, b.IsLiftedToNull, b.Method, b.Conversion);
				}
				return b;
//			}
		}
 
		protected virtual Expression VisitTypeIs(TypeBinaryExpression b)
		{
//			using (IDisposable vmsu = new VisitationMethodStackUpdater(VisitStack, b))
//			{
//				_sb.Append(b.ToString());
				Expression expr = this.Visit(b.Expression);
				if (expr != b.Expression)
					return Expression.TypeIs(expr, b.TypeOperand);
				return b;
//			}
		}
 
		protected virtual Expression VisitConstant(ConstantExpression c)
		{
			// Returning a conversion wrapped around the original constant seemed necessary for certain queries,
			// like a.TimeCreated > new DateTime(15, 3, 12) needed the integers converted. Related to MongoDB storing
			// all fields as strings I think? Currently don't seem to need it but leaving here commented incase issue arises again
//			if (Visited(c))
				return c;
//			return Expression.Convert(Expression.Constant(c.Value, c.Value == null ? typeof(object) : c.Value.GetType()), c.Type);
		}
 
		protected virtual Expression VisitConditional(ConditionalExpression c)
		{
//			using (IDisposable vmsu = new VisitationMethodStackUpdater(VisitStack, c))
//			{
//				_sb.Append(c.ToString());
				Expression test = this.Visit(c.Test);
				Expression ifTrue = this.Visit(c.IfTrue);
				Expression ifFalse = this.Visit(c.IfFalse);
				if (test != c.Test || ifTrue != c.IfTrue || ifFalse != c.IfFalse)
					return Expression.Condition(test, ifTrue, ifFalse);
				return c;
//			}
		}
 
		protected virtual Expression VisitParameter(ParameterExpression p)
		{
			return p;
		}
 
		protected virtual Expression VisitMemberAccess(MemberExpression m)
		{
			Expression exp = this.Visit(m.Expression);
				if (exp == null || (exp.NodeType == ExpressionType.Constant))
					return /*this.Visit*/(Expression.Constant(
						m.Member.DeclaringType.InvokeMember(
						m.Member.Name,
						BindingFlags.Public | BindingFlags.NonPublic
						| BindingFlags.GetField | BindingFlags.GetProperty
						| (exp == null ? BindingFlags.Static : BindingFlags.Instance),
						null,
						exp == null ? null : ((ConstantExpression)exp).Value,
						new object[] { }),
						m.Type));
			if (exp != m.Expression)
				return Expression.MakeMemberAccess(exp, m.Member);
			return m;
		}
 
		protected virtual Expression VisitMethodCall(MethodCallExpression m)
		{
////			if (m.Method.DeclaringType == typeof(System.Linq.Queryable) && m.Method.Name == "Where")
////			{
////				LambdaExpression lambda = (LambdaExpression)
////				StripQuotes
////				(m.Arguments[1]);
////				return Expression.Call(obj, m.Method, Visit(m.Arguments[0]), Visit(m.Arguments[1]));//lambda.Body));
////				
////			}
//			
			ReadOnlyCollection<Expression> args = this.VisitExpressionList(m.Arguments);
			Expression obj = this.Visit(m.Object);
			if (args != m.Arguments || obj != m.Object)
			{
//				if (obj == null || (obj.NodeType == ExpressionType.Constant && obj.Type.IsSpecialName))
//					return /*this.Visit*/(Expression.Constant(
//						m.Method.DeclaringType.InvokeMember(
//						m.Method.Name,
//						BindingFlags.Public | BindingFlags.NonPublic
//						| BindingFlags.InvokeMethod
//						| (obj == null ? BindingFlags.Static : BindingFlags.Instance),
//						null,
//						obj == null ? null : ((ConstantExpression)obj).Value,
//						new object[] { }),
//						m.Type));
				return Expression.Call(obj, m.Method, args);
			}
			return m;
		}
 
		protected virtual Expression VisitLambda(LambdaExpression lambda)
		{
			Expression body = this.Visit(lambda.Body);
			IEnumerable<ParameterExpression> args = this.VisitExpressionList(lambda.Parameters);
			if (body != lambda.Body || args != lambda.Parameters)		// maybe change back by removing args != .. ?
				return Expression.Lambda(lambda.Type, body, args);
			return lambda;
		}
 
		protected virtual Expression VisitNew(NewExpression nex)
		{
			IEnumerable<Expression> args = this.VisitExpressionList(nex.Arguments);
			if (args.All((e) => e.NodeType == ExpressionType.Constant))
				return this.Visit(Expression.Constant(
					nex.Constructor.Invoke(
						args.Cast<ConstantExpression>().Select((c) => c.Value).ToArray()),
					nex.Type));
			if (args != nex.Arguments)
			{
				if (nex.Members != null)
					return Expression.New(nex.Constructor, args, nex.Members);
				else
					return Expression.New(nex.Constructor, args);
			}
			return nex;
		}
 
		protected virtual Expression VisitMemberInit(MemberInitExpression init)
		{
//			using (IDisposable vmsu = new VisitationMethodStackUpdater(VisitStack, init))
//			{
				NewExpression n = (NewExpression)this.VisitNew(init.NewExpression);
				IEnumerable<MemberBinding> bindings = this.VisitBindingList(init.Bindings);
				if (n != init.NewExpression || bindings != init.Bindings)
					return Expression.MemberInit(n, bindings);
				return init;
//			}
		}
 
		protected virtual Expression VisitListInit(ListInitExpression init)
		{
//			using (IDisposable vmsu = new VisitationMethodStackUpdater(VisitStack, init))
//			{
				NewExpression n = (NewExpression)this.VisitNew(init.NewExpression);
				IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(init.Initializers);
				if (n != init.NewExpression || initializers != init.Initializers)
					return Expression.ListInit(n, initializers);
				return init;
//			}
		}
 
		protected virtual Expression VisitNewArray(NewArrayExpression na)
		{
			IEnumerable<Expression> exprs = this.VisitExpressionList(na.Expressions);
			if (exprs.All((e) => e.NodeType == ExpressionType.Constant))
				return Expression.Constant(
					exprs.Cast<ConstantExpression>().Select((c) => c.Value).ToArray().ToArray());
			if (exprs != na.Expressions)
			{
				if (na.NodeType == ExpressionType.NewArrayInit)
					return Expression.NewArrayInit(na.Type.GetElementType(), exprs);
				else
					return Expression.NewArrayBounds(na.Type.GetElementType(), exprs);
			}
			return na;
		}
 
		protected virtual Expression VisitInvocation(InvocationExpression iv)
		{
			IEnumerable<Expression> args = this.VisitExpressionList(iv.Arguments);
			Expression expr = this.Visit(iv.Expression);
			if (args != iv.Arguments || expr != iv.Expression)
				return Expression.Invoke(expr, args);
			return iv;
		}
		#endregion
		
		#region Non-expression visitation methods
		protected virtual MemberBinding VisitBinding(MemberBinding binding)
		{
			switch (binding.BindingType)
			{
			case MemberBindingType.Assignment:
				return this.VisitMemberAssignment((MemberAssignment)binding);
			case MemberBindingType.MemberBinding:
				return this.VisitMemberMemberBinding((MemberMemberBinding)binding);
			case MemberBindingType.ListBinding:
				return this.VisitMemberListBinding((MemberListBinding)binding);
			default:
				throw new Exception(string.Format("Unhandled binding type '{0}'", binding.BindingType));
			}
		}
 
		protected virtual ElementInit VisitElementInitializer(ElementInit initializer)
		{
			ReadOnlyCollection<Expression> arguments = this.VisitExpressionList(initializer.Arguments);
			if (arguments != initializer.Arguments)
			{
				return Expression.ElementInit(initializer.AddMethod, arguments);
			}
			return initializer;
		}
 
		protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
		{
			List<Expression> list = null;
			for (int i = 0, n = original.Count; i < n; i++)
			{
				Expression p = this.Visit(original[i]);
				if (list != null)
				{
					list.Add(p);
				}
				else if (p != original[i])
				{
					list = new List<Expression>(n);
					for (int j = 0; j < i; j++)
					{
						list.Add(original[j]);
					}
					list.Add(p);
				}
			}
			if (list != null)
			{
				return list.AsReadOnly();
			}
			return original;
		}
 
 		protected virtual ReadOnlyCollection<ParameterExpression> VisitExpressionList(ReadOnlyCollection<ParameterExpression> original)
		{
			List<ParameterExpression> list = null;
			for (int i = 0, n = original.Count; i < n; i++)
			{
				ParameterExpression p = (ParameterExpression)this.Visit(original[i]);
				if (list != null)
				{
					list.Add(p);
				}
				else if (p != original[i])
				{
					list = new List<ParameterExpression>(n);
					for (int j = 0; j < i; j++)
					{
						list.Add(original[j]);
					}
					list.Add(p);
				}
			}
			if (list != null)
			{
				return list.AsReadOnly();
			}
			return original;
		}
		
		protected virtual MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			Expression e = this.Visit(assignment.Expression);
			if (e != assignment.Expression)
			{
				return Expression.Bind(assignment.Member, e);
			}
			return assignment;
		}
 
		protected virtual MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
		{
			IEnumerable<MemberBinding> bindings = this.VisitBindingList(binding.Bindings);
			if (bindings != binding.Bindings)
			{
				return Expression.MemberBind(binding.Member, bindings);
			}
			return binding;
		}
 
		protected virtual MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			IEnumerable<ElementInit> initializers = this.VisitElementInitializerList(binding.Initializers);
			if (initializers != binding.Initializers)
			{
				return Expression.ListBind(binding.Member, initializers);
			}
			return binding;
		}
 
		protected virtual IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
		{
			List<MemberBinding> list = null;
			for (int i = 0, n = original.Count; i < n; i++)
			{
				MemberBinding b = this.VisitBinding(original[i]);
				if (list != null)
				{
					list.Add(b);
				}
				else if (b != original[i])
				{
					list = new List<MemberBinding>(n);
					for (int j = 0; j < i; j++)
					{
						list.Add(original[j]);
					}
					list.Add(b);
				}
			}
			if (list != null)
				return list;
			return original;
		}
 
		protected virtual IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
		{
			List<ElementInit> list = null;
			for (int i = 0, n = original.Count; i < n; i++)
			{
				ElementInit init = this.VisitElementInitializer(original[i]);
				if (list != null)
				{
					list.Add(init);
				}
				else if (init != original[i])
				{
					list = new List<ElementInit>(n);
					for (int j = 0; j < i; j++)
					{
						list.Add(original[j]);
					}
					list.Add(init);
				}
			}
			if (list != null)
				return list;
			return original;
		}
		#endregion
	}
}


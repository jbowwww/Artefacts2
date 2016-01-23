using System;
using System.Linq.Expressions;
using System.Text;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Artefacts.Service
{
	public class ExpressionSerializer_ : ExpressionVisitor
	{
		private StringBuilder _sb = new StringBuilder(256);
		
		public ExpressionSerializer()
		{
		}
		
		protected override Expression VisitUnary(UnaryExpression u)
		{
			
		}
		
		protected override Expression VisitBinary(BinaryExpression b)
		{
			return base.VisitBinary(b);
		}
		
		protected override Expression VisitConstant(ConstantExpression c)
		{
			return base.VisitConstant(c);
		}
		
		protected override Expression VisitParameter(ParameterExpression p)
		{
			return base.VisitParameter(p);
		}
		
		protected override Expression VisitTypeIs(TypeBinaryExpression b)
		{
			return base.VisitTypeIs(b);
		}
		
		protected override Expression VisitConditional(ConditionalExpression c)
		{
			return base.VisitConditional(c);
		}
		
		protected override Expression VisitMethodCall(MethodCallExpression m)
		{
			return base.VisitMethodCall(m);
		}
		
		protected override Expression VisitMemberAccess(MemberExpression m)
		{
			return base.VisitMemberAccess(m);
		}
		
		protected override MemberAssignment VisitMemberAssignment(MemberAssignment assignment)
		{
			return base.VisitMemberAssignment(assignment);
		}
		
		protected override Expression VisitMemberInit(MemberInitExpression init)
		{
			return base.VisitMemberInit(init);
		}
		
		protected override MemberListBinding VisitMemberListBinding(MemberListBinding binding)
		{
			return base.VisitMemberListBinding(binding);
		}
		
		protected override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding binding)
		{
			return base.VisitMemberMemberBinding(binding);
		}

		protected override Expression VisitNew(NewExpression nex)
		{
			return base.VisitNew(nex);
		}
		
		protected override Expression VisitNewArray(NewArrayExpression na)
		{
			return base.VisitNewArray(na);
		}
		
		protected override MemberBinding VisitBinding(MemberBinding binding)
		{
			return base.VisitBinding(binding);
		}
		
		protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
		{
			return base.VisitBindingList(original);
		}
		
		protected override ElementInit VisitElementInitializer(ElementInit initializer)
		{
			return base.VisitElementInitializer(initializer);
		}
		
		protected override IEnumerable<ElementInit> VisitElementInitializerList(ReadOnlyCollection<ElementInit> original)
		{
			return base.VisitElementInitializerList(original);
		}
		
		protected override ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
		{
			return base.VisitExpressionList(original);
		}
		
		protected override ReadOnlyCollection<ParameterExpression> VisitExpressionList(ReadOnlyCollection<ParameterExpression> original)
		{
			return base.VisitExpressionList(original);
		}
		
		protected override Expression VisitListInit(ListInitExpression init)
		{
			return base.VisitListInit(init);
		}
		
		protected override Expression VisitInvocation(InvocationExpression iv)
		{
			return base.VisitInvocation(iv);
		}
		
		protected override Expression VisitLambda(LambdaExpression lambda)
		{
			return base.VisitLambda(lambda);
		}
	}
}


using System;
using System.Linq.Expressions;
using Serialize.Linq.Nodes;
using System.Collections;
using System.Linq;

namespace Artefacts
{
	/// <summary>
	/// Expression_ ext.
	/// </summary>
	public static class ExpressionExtensions
	{
		/// <summary>
		/// Identifier the specified expression.
		/// </summary>
		/// <param name="expression">Expression.</param>
		/// <remarks>
		/// Currently not used. Either get rid of or if needed change to return ServiceStack.ObjectId
		/// </remarks>
		public static int Id(this Expression expression)
		{
			return expression.GetHashCode();
		}

		/// <summary>
		/// Determines if is enumerable the specified expression.
		/// </summary>
		/// <returns><c>true</c> if is enumerable the specified expression; otherwise, <c>false</c>.</returns>
		/// <param name="expression">Expression.</param>
		public static bool IsEnumerable(this Expression expression)
		{
			return expression.Type.GetInterface("IEnumerable") != null;
		}

		/// <summary>
		/// Gets the type of the element.
		/// </summary>
		/// <returns>The element type.</returns>
		/// <param name="expression">Expression.</param>
		public static Type GetElementType(this Expression expression)
		{
			if (expression.Type.IsArray)
				return expression.Type.GetElementType();
			if (typeof(IEnumerable).IsAssignableFrom(expression.Type)
			 && expression.Type.GetGenericArguments().Length > 0)
				return expression.Type.GetGenericArguments()[0];
			if (expression.Type.GetInterface("IEnumerable") != null)
				return typeof(object);
			throw new ArgumentOutOfRangeException("expression", string.Concat(
				"Could not get an element type from expression.Type=\"",
				expression.Type.FullName, "\""));
		}
		
		/// <summary>
		/// Tries the type of the get element.
		/// </summary>
		/// <returns><c>true</c>, if get element type was tryed, <c>false</c> otherwise.</returns>
		/// <param name="Expression">Expression.</param>
		/// <param name="elementType">Element type.</param>
		public static bool TryGetElementType(this Expression expression, out Type elementType)
		{
			elementType = null;
			if (expression.Type.IsArray)
				elementType = expression.Type.GetElementType();
			else if (typeof(IEnumerable).IsAssignableFrom(expression.Type)
			 &&	expression.Type.GetGenericArguments().Length > 0)
				elementType = expression.Type.GetGenericArguments()[0];
			else if (expression.Type.GetInterface("IEnumerable") != null)
				elementType = typeof(object);
			else if (expression.NodeType == ExpressionType.Call)
			{
				MethodCallExpression m = (MethodCallExpression)expression;
				if (m.Arguments.Count == 0
				 || (m.Arguments[0].NodeType != ExpressionType.Call
				  && !TryGetElementType(m.Arguments[0], out elementType)))
					return false;
			}
			return true;
		}
		
		/// <summary>
		/// Gets the type of the root element.
		/// </summary>
		/// <param name="expression">Expression.</param>
		/// <returns>The root element type.</returns>
		public static Type GetRootElementType(this Expression expression)
		{
			Type elementType = null;
			if (expression.NodeType == ExpressionType.Call)
				elementType = ((MethodCallExpression)expression).Arguments[0].GetRootElementType();
			else if (expression.Type.IsArray)
				elementType = expression.Type.GetElementType();
			else if (typeof(IEnumerable).IsAssignableFrom(expression.Type)
			         && expression.Type.GetGenericArguments().Length > 0)
				elementType = expression.Type.GetGenericArguments()[0];
			else if (expression.Type.GetInterface("IEnumerable") != null)
				elementType = typeof(object);
			return elementType;
		}
		
		/// <summary>
		/// Tos the expression node.
		/// </summary>
		/// <returns>The expression node.</returns>
		/// <param name="expression">Expression.</param>
		public static ExpressionNode ToExpressionNode(this Expression expression)
		{
			return Serialize.Linq.Extensions.ExpressionExtensions.ToExpressionNode(expression);
		}
		
		/// <summary>
		/// Tos the string.
		/// </summary>
		/// <param name="expression">Expression.</param>
		/// <returns>The string.</returns>
		public static string ToString(this Expression expression)
		{
			return expression.ToString();
			// ExpressionNode.ToString() converts back to a System.Linq.Expression
			//return Serialize.Linq.Extensions.ExpressionExtensions.ToExpressionNode().ToString();
		}
	}
}


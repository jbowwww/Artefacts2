using System;
using System.Linq;
using System.Linq.Expressions;
using ServiceStack;

using Serialize.Linq.Nodes;

namespace Artefacts.Service
{
	public class SaveArtefactRequest<T> : IReturn<string>
	{
		public string Data { get; set; }

		public LambdaExpression Match {
			get { return Data == null ? null : (LambdaExpression)Data.FromJson<ExpressionNode>().ToExpression(); }
			set { Data = value.ToExpressionNode().ToJson<ExpressionNode>(); }
		}
		
		public SaveArtefactRequest(Expression<Func<T, bool>> match, T instance)
		{
			
		}
	}
}


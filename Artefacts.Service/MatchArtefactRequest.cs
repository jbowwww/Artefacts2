using System;
using Artefacts;
using System.IO;
using System.Web;
using System.Net;
using ServiceStack;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using Serialize.Linq.Serializers;
using Serialize.Linq;
using Serialize.Linq.Nodes;
using Serialize.Linq.Extensions;
using Serialize.Linq.Interfaces;

namespace Artefacts.Service
{
	[DataContract]
	[Route("/artefacts/{Data}", "GET")]
	public class MatchArtefactRequest
	{
		static private ExpressionVisitor _expressionVisitor = new ClientQueryVisitor();

		private ITextSerializer _serializer = null;
		private ExpressionVisitor _visitor = null;
		
		[DataMember(Order=1)]
		public string Data { get; set; }
		
		public Expression Match {
			get
			{
				return Data == null ? null : Data.FromJson<ExpressionNode>().ToExpression();
			}
			set
			{
				Data = _serializer.Serialize(_visitor.Visit(value).ToExpressionNode());
				//.ToJson<ExpressionNode>();
				//	new JsonSerializer().Serialize<ExpressionNode>(value.ToExpressionNode());
			}
		}
		
		public MatchArtefactRequest(Expression match = null, ExpressionVisitor visitor = null, ITextSerializer serializer = null)
		{
			this._serializer = serializer ?? new JsonSerializer();
			this._visitor = visitor ?? _expressionVisitor;	// new ClientQueryVisitor();
			this.Match = match ?? Expression.Default(typeof(bool));
		}
	}
}


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

namespace Artefacts.Service
{
	[DataContract]
	[Route("/artefacts/{Data}", "GET")]
	public class MatchArtefactRequest
	{
		[DataMember(Order=1)]
		public string Data { get; set; }
		
		public Expression Match {
			get
			{
				return Data == null ? null : Data.FromJson<ExpressionNode>().ToExpression();
			}
			set
			{
				Data = value.ToExpressionNode().ToJson<ExpressionNode>();
					new JsonSerializer().Serialize<ExpressionNode>(value.ToExpressionNode());
			}
		}
		
		public MatchArtefactRequest() {}
	}
}


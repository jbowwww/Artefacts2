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
	public class MatchArtefactRequest : IReturn<Artefact>
	{
		static private ExpressionVisitor _expressionVisitor = new ClientQueryVisitor();

		private ISerializer _serializer = null;
		private ExpressionVisitor _visitor = null;
		
		[DataMember(Order=1)]
		public string Data { get; set; }
		
//		[DataMember]
//		public string DataExperimental { get; set; }
		
		public Expression Match {
			get
			{
				ExpressionNode node =
//				_serializer.Deserialize<ExpressionNode>(Data);
//				Expression node = 
					Data.FromJson<ExpressionNode>();
//					_serializer.Deserialize<Expression>(Data);
				return Data == null ? null : node.ToExpression(); //Data.FromJson<ExpressionNode>().ToExpression();
//				return DataExperimental == null ? null : Expression.MakeDynamic(
			}
			set
			{
				Data = _visitor.Visit(value).ToExpressionNode().ToJson<ExpressionNode>();
//					_serializer.Serialize(_visitor.Visit(value));//.ToExpressionNode());
				//.ToJson<ExpressionNode>();
				//	new JsonSerializer().Serialize<ExpressionNode>(value.ToExpressionNode());
			}
		}
		
		public MatchArtefactRequest() : this(null, null, null) {}
		public MatchArtefactRequest(Expression match = null, ExpressionVisitor visitor = null, ITextSerializer serializer = null)
		{
//			this._serializer = serializer ?? new ExpressionSerializer(new JsonSerializer());
//				new JsonSerializer();
//			_serializer.AddKnownTypes(new Type[] {
//				typeof(ExpressionNode),
//				typeof(ParameterExpressionNode),
//				typeof(Expression),
//				typeof(Func<>),
//				typeof(Artefact),
//				typeof(Artefacts.FileSystem.Disk),
//				typeof(Artefacts.Host)
//			});
			this._visitor = visitor ?? _expressionVisitor ?? new ClientQueryVisitor();
			this.Match = match;// ?? Expression.Default(typeof(bool));
		}
	}
}


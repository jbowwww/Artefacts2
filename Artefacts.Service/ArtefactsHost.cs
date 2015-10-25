
using ServiceStack;
using ServiceStack.Web;
using ServiceStack.Logging;
using System.CodeDom.Compiler;
//using Serialize.Linq.Nodes;
using MongoDB.Bson;
using System.Collections.Generic;
using System;
using System.IO;
using ServiceStack.Text;
using System.Dynamic;
using System.Text;
using System.Threading;
using Serialize.Linq.Nodes;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Artefacts.Service
{
	/// <summary>
	/// Artefacts host.
	/// </summary>
	public class ArtefactsHost : AppHostHttpListenerBase
	{
		#region Static members
		private static bool _appHostThreadExit = false;

		protected static ArtefactsHost AppHost = null;

		public static void StartHost(string serviceBaseUrl, TextWriter output)
		{
//			StringBuilder sb = new StringBuilder(4096);
			new Thread(() =>  {
				AppHost = new ArtefactsHost(output); /*new StringWriter(sb)*/ 
				output.Write(string.Format("Starting application host at {0} ... ", serviceBaseUrl));
				AppHost.Init().Start(serviceBaseUrl);
				output.WriteLine("OK");
				while (!System.Threading.Volatile.Read(ref _appHostThreadExit))	// || sb.Length > 0)
				{
//					string s = sb.ToString();
//					sb.Clear();
//					tvHost.Buffer.InsertAtCursor(s);
					Thread.Sleep(248);
				}
				output.Write("Application host exit flag set, exiting ... ");
				AppHost.Stop();
				output.WriteLine("OK");
			}) {
				Priority = ThreadPriority.BelowNormal
			}.Start();
		}
		
		public static void StopHost()
		{
			System.Threading.Volatile.Write(ref _appHostThreadExit, true);
		}
		#endregion // Static members
		
		private TextWriter _output = null;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Host.ArtefactsHost"/> class.
		/// </summary>
		public ArtefactsHost(TextWriter output)
			: base("Artefacts",
			       typeof(ArtefactsService).Assembly,
//			       typeof(Artefact).Assembly,
			       typeof(Func<>).Assembly,
//			       typeof(Dictionary<string,object>).Assembly,
//			typeof(BsonDocument).Assembly,
//			       typeof(DynamicObject).Assembly,
			typeof(ExpressionNode).Assembly,
			       typeof(IMongoQuery).Assembly,
			       typeof(BsonDocument).Assembly,
			       typeof(Expression).Assembly,
			       typeof(Artefacts.Host).Assembly,
			typeof(Artefacts.FileSystem.Disk).Assembly)
		{
			_output = output;
//			JsConfig.ConvertObjectTypesIntoStringDictionary = true;
//			JsConfig.Dump();
		}

		/// <summary>
		/// Configure the specified container.
		/// </summary>
		/// <param name="container">Container.</param>
		/// <remarks>
		/// implemented abstract members of ServiceStackHost
		/// </remarks>
		public override void Configure(Funq.Container container)
		{
			//			ServiceController
			//			IServiceController isc;
			SetConfig(new HostConfig()
			          {
				WriteErrorsToResponse = true,
				DebugMode = true,
				ReturnsInnerException = true,
				AllowPartialResponses = true,
//				Return204NoContentForEmptyResponse = true,
				EmbeddedResourceBaseTypes = new List<Type>(new Type[] {
//					typeof(DynamicObject), typeof(Dictionary<string,object>),
//					typeof(ArtefactData),
//					typeof(Artefact), typeof(BsonValue),
//					typeof(Artefacts.FileSystem.Disk),
//					typeof(Artefacts.FileSystem.Drive),
//					typeof(Artefacts.FileSystem.Directory),
//					typeof(Artefacts.FileSystem.File),
//					typeof(Artefacts.FileSystem.FileSystemEntry)
				})
//				UseBclJsonSerializers = true		// So I think I only need this currently because I am using a dynamic
													// type as my DTO. When I start using [CRUD]Artefact request DTOs, they
													// will be non-dynamic and contain a dictionary of member values.
													// That could still be implemented in a number of ways but I think that
													// is the general approach needed.
			});
			
			container.Register<ArtefactsService>(new ArtefactsService(_output));
//			this.Routes
//				.Add<Artefact>("/artefacts/
//				.Add<Artefact>("/artefacts", ApplyTo.Put)
////						.Add<dynamic>("/artefacts", ApplyTo.Put)
//
//				.Add<BsonDocument>("/artefacts_asBson", ApplyTo.Put)
//					.Add<MatchArtefactRequest>("/artefacts/match", ApplyTo.Get);
//				.Add<BsonDocument>("/docs", ApplyTo.Put | ApplyTo.Post | ApplyTo.Update | ApplyTo.Delete)
//				.Add<byte[]>("/bytes", ApplyTo.Put)
//				.Add<string>("/strings", ApplyTo.Put);

			//					//				.Add<ObjectId>("/artefacts/Id={ToString}", ApplyTo.Get)
//					.Add<GetQueryRequest>("/artefacts/Query={Expression}", ApplyTo.Get)
//					.Add<GetArtefactById>("/artefacts/Id={Id}", ApplyTo.Get);
			//			this.
			//			.Add<QueryableRequest>("/artefacts/query")
			//					.Add<Queryable<Artefact>>("/artefacts/query2");
			//					.Add<ArtefactAddAspectRequest>("/aspects", ApplyTo.Put);
			//					.Add<Artefact>("/artefact/{Id}")
			//					.Add<>();
			//			this.AppSettings.GetListc
		}
	}
}


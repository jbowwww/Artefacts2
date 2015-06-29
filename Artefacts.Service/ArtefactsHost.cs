
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

namespace Artefacts.Service
{
	/// <summary>
	/// Artefacts host.
	/// </summary>
	public class ArtefactsHost : AppHostHttpListenerBase
	{
		private TextWriter _output = null;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Host.ArtefactsHost"/> class.
		/// </summary>
		public ArtefactsHost(TextWriter output)
			: base("Artefacts",
			       typeof(ArtefactsService).Assembly,
			       typeof(Artefact).Assembly,
			typeof(BsonDocument).Assembly)
		{
			_output = output;
			JsConfig.ConvertObjectTypesIntoStringDictionary = true;
//			JsConfig.Dump<JsConfig>();
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
				Return204NoContentForEmptyResponse = true
//				EmbeddedResourceBaseTypes = new List<Type>(new Type[] {
//					typeof(Artefact), typeof(BsonValue) })
//					typeof(Artefacts.FileSystem.Disk),
//					typeof(Artefacts.FileSystem.Drive),
//					typeof(Artefacts.FileSystem.Directory),
//					typeof(Artefacts.FileSystem.File),
//					typeof(Artefacts.FileSystem.FileSystemEntry)});
			});
			
			container.Register<ArtefactsService>(new ArtefactsService(_output));
			this.Routes
				.Add<Artefact>("/artefacts", ApplyTo.Put);
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


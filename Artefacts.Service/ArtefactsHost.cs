
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
	public class ArtefactsHost : AppHostHttpListenerBase, IDisposable
	{
		#region Static members
		public static readonly ILogFactory LogFactory;
		public static readonly ILog Log;
		
		/// <summary>
		/// Initializes the <see cref="Artefacts.Service.ArtefactsHost"/> class.
		/// </summary>
		static ArtefactsHost()
		{
			LogFactory = new StringBuilderLogFactory();
			Log = ArtefactsHost.LogFactory.GetLogger(typeof(ArtefactsHost));
		}
		
		/// <summary>
		/// Gets the singleton.
		/// </summary>
		/// <value>The singleton.</value>
		public static ArtefactsHost Singleton {
			get;
			private set;
		}
		#endregion // Static members
		
		#region Private fields
		/// <summary>
		/// The thread sleep time, in milliseconds
		/// </summary>
		private const int ThreadSleepTime = 615;
		
		private TextWriter _output;
		
		private bool _appHostThreadExit;
		
		private Thread _appHostThread;
		#endregion
		
		/// <summary>
		/// Gets a value indicating whether this instance is running.
		/// </summary>
		public bool IsRunning {
			get { return !_appHostThreadExit; }
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Host.ArtefactsHost"/> class.
		/// </summary>
		public ArtefactsHost(string serviceBaseUrl, TextWriter output)
			: base("Artefacts",
			       //typeof(ArtefactsService).Assembly,
			       typeof(Artefact).Assembly,
			       typeof(Func<>).Assembly,
			typeof(ExpressionNode).Assembly,
			       typeof(IMongoQuery).Assembly,
			       typeof(BsonDocument).Assembly,
			       typeof(Expression).Assembly,
			       typeof(Artefacts.Host).Assembly,
			typeof(Artefacts.FileSystem.Disk).Assembly)
		{
			Log.DebugFormat("new ArtefactsHost(\"{0}\", {1})", serviceBaseUrl, output.ToString());
			_output = output;
			Log.InfoFormat("Starting application host at \"{0}\"", serviceBaseUrl);
			output.Write(string.Format("Starting application host at {0} ... ", serviceBaseUrl));
			if (Singleton != null)
				throw new InvalidOperationException("Singleton instance already exists");
			Singleton = this;
			Log.Debug("ArtefactsHost.Init()");
			base.Init();
			Log.DebugFormat("ArtefactsHost.Start(\"{0}\")", serviceBaseUrl);
			base.Start(serviceBaseUrl);
			output.WriteLine("OK");
			_appHostThread = new Thread(() => { Run(); }) { Priority = ThreadPriority.BelowNormal };
			Log.DebugFormat("new Thread(() => ArtefactsHost.Run()) {{ Priority = {0} }}.Start()", _appHostThread.Priority);
			_appHostThread.Start();
		}

		/// <summary>
		/// Dispose the specified disposing.
		/// </summary>
		/// <param name="disposing">If set to <c>true</c> disposing.</param>
		protected override void Dispose(bool disposing)
		{
			Log.DebugFormat("Dispose({0})", disposing);
			if (IsRunning)
				Stop();
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// Releases all resource used by the <see cref="Artefacts.Service.ArtefactsHost"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Artefacts.Service.ArtefactsHost"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Artefacts.Service.ArtefactsHost"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Artefacts.Service.ArtefactsHost"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Artefacts.Service.ArtefactsHost"/> was occupying.</remarks>
		void Dispose()
		{
			Dispose(true);
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
		
		/// <summary>
		/// Run this instance.
		/// </summary>
		private void Run()
		{
			Log.Info("ArtefactsHost.Run(): Start");
			while (!Volatile.Read(ref _appHostThreadExit))
			{
				Thread.Sleep(ThreadSleepTime);
			}
			Log.Info("ArtefactsHost.Run(): Return");
		}
		
		/// <summary>
		/// Stop this instance.
		/// </summary>
		private void Stop()
		{
			Log.Debug("ArtefactsHost.Stop()");
			if (Volatile.Read(ref _appHostThreadExit))
				throw new InvalidOperationException("Stop() has already been called");
			base.Stop();
			Volatile.Write(ref _appHostThreadExit, true);
		}
	}
}


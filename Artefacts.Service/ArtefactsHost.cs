using System;
using System.IO;
using System.Threading;
using ServiceStack;
using ServiceStack.Logging;

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
		#endregion
		
		#region Private fields
		private const int ThreadSleepTime = 777;
		private TextWriter _output;
		private byte _appHostThreadExit;
		private Thread _appHostThread;
		#endregion
		
		/// <summary>
		/// Gets a value indicating whether this instance is running.
		/// </summary>
		public bool IsRunning {
			get { return _appHostThread != null && _appHostThread.IsAlive && (Thread.VolatileRead(ref _appHostThreadExit)==0); }
		}
		
		/// <summary>
		/// Gets the service.
		/// </summary>
		public ArtefactsService Service {
			get;
			private set;
		}
		
		#region Construction & disposal
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Host.ArtefactsHost"/> class.
		/// </summary>
		public ArtefactsHost(string serviceBaseUrl, TextWriter output)
			: base("Artefacts", typeof(Artefact).Assembly,
				typeof(Artefacts.Service.ArtefactsService).Assembly,
				typeof(Artefacts.FileSystem.Disk).Assembly)
		{
			if (Singleton != null)
				throw new InvalidOperationException("Singleton instance already exists");
			Singleton = this;
			_output = output;
			Log.InfoFormat("Starting application host at \"{0}\"", serviceBaseUrl);
			output.Write(string.Format("Starting application host at {0} ... ", serviceBaseUrl));
			Log.Debug("ArtefactsHost.Init()");
			base.Init();
			Log.DebugFormat("ArtefactsHost.Start(\"{0}\")", serviceBaseUrl);
			base.Start(serviceBaseUrl);
			output.WriteLine("OK");
			//_appHostThread = new Thread(() => { Run(); }) { Priority = ThreadPriority.Lowest };	//.BelowNormal };
			//Run();
			//Log.DebugFormat("new Thread(() => ArtefactsHost.Run()) {{ Priority = {0} }}.Start()", _appHostThread.Priority);
			//_appHostThread.Start();
		}

		/// <summary>
		/// Dispose the specified disposing.
		/// </summary>
		/// <param name="disposing">If set to <c>true</c> disposing.</param>
		protected override void Dispose(bool disposing)
		{
			Log.DebugFormat("Dispose({0})", disposing);
			Log.Info("Disposing ArtefactsHost");
			if (IsRunning)
				Stop();
		}
		
		/// <summary>
		/// Releases all resource used by the <see cref="Artefacts.Service.ArtefactsHost"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Artefacts.Service.ArtefactsHost"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Artefacts.Service.ArtefactsHost"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Artefacts.Service.ArtefactsHost"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Artefacts.Service.ArtefactsHost"/> was occupying.</remarks>
		public override void Dispose()
		{
			Dispose(true);
			base.Dispose();
		}
		#endregion
		
		/// <summary>
		/// Configure the specified container.
		/// </summary>
		/// <param name="container">Container.</param>
		/// <remarks>
		/// implemented abstract members of ServiceStackHost
		/// </remarks>
		public override void Configure(Funq.Container container)
		{
			SetConfig(new HostConfig()
			{
				WriteErrorsToResponse = true,
				DebugMode = true,
				ReturnsInnerException = true
//				AllowPartialResponses = true,
			
			});
			Service = new ArtefactsService(_output);
			container.Register<ArtefactsService>(Service);
		}
		
		/// <summary>
		/// Run this instance.
		/// </summary>
		private void Run()
		{
			Log.Info("ArtefactsHost.Run(): Start");
			while (Thread.VolatileRead(ref _appHostThreadExit) == 0)
			{
//				Semaphore.WaitAny();
				Thread.Sleep(ThreadSleepTime);
			}
			Log.Info("ArtefactsHost.Run(): Return");
		}
		
		/// <summary>
		/// Stop this instance.
		/// </summary>
		private new void Stop()
		{
			Log.Debug("ArtefactsHost.Stop()");
			if (Thread.VolatileRead(ref _appHostThreadExit) == 1)
				throw new InvalidOperationException("Stop() has already been called");
			base.Stop();
			Thread.VolatileWrite(ref _appHostThreadExit, 1);
		}
	}
}


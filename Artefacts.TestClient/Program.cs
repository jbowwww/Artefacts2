using System;
using Gtk;
using Artefacts;
using Artefacts.Service;
using System.Threading;
using ServiceStack.Logging;

namespace Artefacts.TestClient
{
	class MainClass
	{
		public static readonly ILog Log;
		
		public static string serviceBaseUrl = "http://localhost:8888/Artefacts/";
		
		public static TextBufferWriter ClientWriter { get; private set; }

		public static TextBufferWriter HostWriter { get; private set; }
		
		static MainClass()
		{
			Log = new DebugLogger(typeof(MainClass));// Artefact.LogFactory.GetLogger(typeof(MainClass));
		}
		
		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name="args">The command-line arguments.</param>
		/// <remarks>
		/// Client test code - need to figure out how to use TestFixtureAttirbute (et al) properly, with parameters etc
		/// </remarks>
		public static void Main(string[] args)
		{
			try
			{
				Log.Debug("Application.Init()");
				Application.Init();
				Log.Debug("win = new MainWindow()");
				MainWindow win = new MainWindow();
				
				HostWriter = new TextBufferWriter(win.HostTextBuffer, win);
				ClientWriter = new TextBufferWriter(win.ClientTextBuffer, win);

				Log.Debug("win.Show()");
				win.Show();
				
				new Thread(() => {
					using (ArtefactsHost Host = new ArtefactsHost(serviceBaseUrl, HostWriter))
					{
						Thread.Sleep(1111);
	//					Host.Start(serviceBaseUrl);
	//					Thread.Sleep(888);
						
						using (ArtefactsTestClient Client = new ArtefactsTestClient(serviceBaseUrl, ClientWriter, win))
						{
							Thread.Sleep(481);
							Client.Run();
						}
					}
				}).Start();
				
				Thread.Sleep(888);
				Log.Debug("Application.Run()");
				Application.Run();
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
			finally
			{
				
			}
		}
	}
}

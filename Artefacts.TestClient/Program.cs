using System;
using Gtk;
using Artefacts;
using Artefacts.Service;
using System.Threading;
using ServiceStack.Logging;
using System.Configuration;

namespace Artefacts.TestClient
{
	class MainClass
	{
		public static readonly ILog Log;
		
		public static ConfigurationSettings Settings { get; private set; }
		
		public static string serviceBaseUrl = "http://localhost:8888/Artefacts/";
		
		public static TextBufferWriter ClientWriter { get; private set; }

		public static TextBufferWriter HostWriter { get; private set; }
		
		private static bool _quit = false;
		
		public static void Quit()
		{
//			Artefacts.FileSystem.File.CRCAbortThread();
			Volatile.Write(ref _quit, true);
		}
		
		public static bool HasQuit()
		{
			return Volatile.Read(ref _quit);
		}
		
		static MainClass()
		{
			Log = new ConsoleLogger(typeof(MainClass));	// DebugLogger(typeof(MainClass));// Artefact.LogFactory.GetLogger(typeof(MainClass));
//			ConfigurationManager.AppSettings;
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
				Thread clientThread = null;
				
				Log.Debug("Application.Init()");
				Application.Init();

				GLib.ExceptionManager.UnhandledException += (GLib.UnhandledExceptionArgs exArgs) => {
					Exception /*object GLib.GException*/ ge = exArgs.ExceptionObject as Exception;// as GLib.GException;
					if (ge != null)
						Log.ErrorFormat("GLib unhandled {0}:\n\tExitApplication={1}, IsTerminating={2}\n{3}",
							ge.GetType().FullName, exArgs.ExitApplication, exArgs.IsTerminating, ge.Format());
					else if (exArgs.ExceptionObject != null)
						Log.ErrorFormat("GLib unhandled {0}:\n\tExitApplication={1}, IsTerminating={2}\n{3}",
							exArgs.ExceptionObject.GetType().FullName, exArgs.ExitApplication, exArgs.IsTerminating, ge.Format());
					else
						Log.ErrorFormat("GLib unhandled exception, args.ExceptionObject=null:\n\tExitApplication={1}, IsTerminating={2}\n{3}",
							exArgs.ExitApplication, exArgs.IsTerminating, ge.Format());
				};
				
				Log.Debug("win = new MainWindow()");
				MainWindow win = new MainWindow();
				
				HostWriter = new TextBufferWriter(win.HostTextBuffer, win);
				ClientWriter = new TextBufferWriter(win.ClientTextBuffer, win);

				Log.Debug("win.Show()");
				win.Show();
				
				win.DeleteEvent += (o, a2) => {
					if (clientThread != null && clientThread.IsAlive)
						clientThread.Abort();
					
					MainClass.Quit();
					Thread.Sleep(111);
					ClientWriter.WriteLine("Client thread might have exited by now ?? :)!");
					
					Application.Quit();
				};
				
//				new Thread(() => {
					using (ArtefactsHost Host = new ArtefactsHost(serviceBaseUrl, HostWriter))
					{
	//						win.DeleteEvent += (o, a1) => { Host.Release(); };
	
	//						Thread.Sleep(1111);
		//					Host.Start(serviceBaseUrl);
						win.OnBtnStartClicked += (object sender, EventArgs e) => 
						{
							clientThread = new Thread(() => {
								try
								{
									using (ArtefactsTestClient Client = new ArtefactsTestClient(serviceBaseUrl, ClientWriter, win))
									{
										Client.Run();
									}
								}
								catch (Exception ex)
								{
									Log.Error(ex);
									ClientWriter.WriteLine(ex);
								}
								finally
								{
									clientThread = null;	
								}
							});
							clientThread.Start();
						};
	//					Thread.Sleep(888);
						Log.Debug("Application.Run()");
	 					Application.Run();
					}
				//}).Start();
			}
			catch (Exception ex)
			{
				Log.Error(ex);
			}
			finally
			{
				Log.Info("Quitting");
			}
		}
	}
}

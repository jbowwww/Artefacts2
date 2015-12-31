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
		
		private static bool _quit = false;
		
		public static void Quit()
		{
			Volatile.Write(ref _quit, true);
		}
		
		public static bool HasQuit()
		{
			return Volatile.Read(ref _quit);
		}
		
		static MainClass()
		{
			Log = new ConsoleLogger(typeof(MainClass));	// DebugLogger(typeof(MainClass));// Artefact.LogFactory.GetLogger(typeof(MainClass));
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
				
				Thread clientThread = null;
				win.DeleteEvent += (o, a2) => {
					if (clientThread != null && clientThread.IsAlive)
						clientThread.Abort();

					Artefacts.FileSystem.File.CRCAbortThread(true);
					ClientWriter.WriteLine("Artefacts.FileSystem.File CRC Thread cancelled!");
					
					MainClass.Quit();
					Thread.Sleep(111);
					ClientWriter.WriteLine("Client thread might have exited by now ?? :)!");
					
					Application.Quit();
				};
				
//				new Thread(() => {
					using (ArtefactsHost Host = new ArtefactsHost(serviceBaseUrl, HostWriter))
					{
//						win.DeleteEvent += (o, a1) => { Host.Release(); };

						Thread.Sleep(1111);
	//					Host.Start(serviceBaseUrl);
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
					
					Thread.Sleep(888);
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

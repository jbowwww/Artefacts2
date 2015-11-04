using System;
using System.Linq;
using Gtk;
using Artefacts;
using Artefacts.Service;
using System.Reflection;
using NUnit.Framework;
using NUnit;
using ServiceStack;
using ServiceStack.Logging;
using System.Collections.Generic;
using System.Threading;

namespace Artefacts.TestClient
{
	class MainClass
	{
		public static readonly ILog Log;
		
		public const string ServiceBaseUrl = "http://localhost:8888/";

		public static ArtefactsHost Host;
		
		public static ArtefactsTestClient Client;
			
		static MainClass()
		{
			Log = ArtefactsClient.LogFactory.GetLogger(typeof(MainClass));
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
				Log.Debug("win.Show()");
				win.Show();
				new Thread(() => {
					using (Host = new ArtefactsHost(ServiceBaseUrl, win.HostWriter))
					using (Client = new ArtefactsTestClient(ServiceBaseUrl, win.ClientWriter))
					{
						//Thread.Sleep(1100);
						Client.Run();
					}
				}).Start();
				//Thread.Sleep(888);
				Application.Run();
				Log.Debug("Application.Run()");
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

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
			Application.Init();
			Log.Debug("Application.Init()");
			MainWindow win = new MainWindow();
			Log.Debug("win = new MainWindow()");
			win.Show();
			Log.Debug("win.Show()");
			
			using (Host = new ArtefactsHost(ServiceBaseUrl, new TextBufferWriter(win.HostTextBuffer)))
			{
				Thread.Sleep(1100);
				TextBufferWriter clientWriter = new TextBufferWriter(win.ClientTextBuffer);
				ArtefactsTestClient client = new ArtefactsTestClient(ServiceBaseUrl, clientWriter);
				IEnumerable<MethodInfo> testMethods = typeof(ArtefactsTestClient).GetMethods().Where(mi => mi.GetCustomAttribute<TestAttribute>() != null);
				foreach (MethodInfo mi in testMethods)
				{
					string testName = string.Concat(mi.DeclaringType.FullName, ".", mi.Name);
					try
					{
						Log.InfoFormat("\n--------\nTest: {0}\n--------", testName);
						clientWriter.WriteLine("\n--------\nTest: {0}\n--------", testName);
						mi.Invoke(client, new object[] { });
					}
					//					catch (WebServiceException e) { clientWriter.WriteLine("ERROR: " + ex.Format()); }
					//					catch (TargetInvocationException e) { clientWriter.WriteLine("ERROR: " + ex.Format()); }
					catch (Exception ex) {
						Log.Error("! ERROR: ", ex);
						clientWriter.WriteLine("! ERROR: " + ex.Format());
					}
					finally
					{
						Log.InfoFormat("--------\nFinished: {0}\n--------", testName);
						clientWriter.WriteLine("--------\nFinished: {0}\n--------", testName);
					}
				}
			}
			
			Application.Run();
			Log.Debug("Application.Run()");
//			client.PutArtefact();
//			client.PutArtefactData();
//			client.PutArtefactAlternativeSerializations();
//			client.PutArtefact_Disk_New();
			
//			ArtefactsHost.StopHost();
		}
	}
}

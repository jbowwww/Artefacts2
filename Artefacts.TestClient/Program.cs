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

namespace Artefacts.TestClient
{
	class MainClass
	{
		public static readonly ILogFactory LogFactory;
		public static readonly ILog Log;
		
		public const string ServiceBaseUrl = "http://localhost:8888/";

		static MainClass()
		{
			LogFactory = new StringBuilderLogFactory();
			Log = LogManager.GetLogger(typeof(MainClass));	
		}
		
		public static void Main(string[] args)
		{
			Application.Init();
			MainWindow win = new MainWindow();
			win.Show();
			
			ArtefactsHost.StartHost(ServiceBaseUrl, new TextBufferWriter(win.HostTextBuffer));
			System.Threading.Thread.Sleep(1100);
			
			/* Client test code - need to figure out how to use TestFixtureAttirbute (et al) properly, with parameters etc */
			TextBufferWriter clientWriter = new TextBufferWriter(win.ClientTextBuffer);
			ArtefactsTestClient client = new ArtefactsTestClient(ServiceBaseUrl, clientWriter);
			foreach (MethodInfo mi in typeof(ArtefactsTestClient).GetMethods().Where(mi => mi.GetCustomAttribute<TestAttribute>() != null))
			{
				try
				{
					clientWriter.WriteLine("\n--------\nTest: {0}.{1}\n--------", mi.DeclaringType.FullName, mi.Name);
					mi.Invoke(client, new object[] { });
				}
//				catch (WebServiceException e) { clientWriter.WriteLine("ERROR: " + ex.Format()); }
//				catch (TargetInvocationException e) { clientWriter.WriteLine("ERROR: " + ex.Format()); }
				catch (Exception ex) { clientWriter.WriteLine("! ERROR: " + ex.Format()); }
				finally
				{
					clientWriter.WriteLine("--------\n");
				}
			}
			
//			client.PutArtefact();
//			client.PutArtefactData();
//			client.PutArtefactAlternativeSerializations();
//			client.PutArtefact_Disk_New();
			
			Application.Run();

			ArtefactsHost.StopHost();
		}
	}
}

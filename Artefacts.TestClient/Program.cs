using System;
using Gtk;
using Artefacts.Service;

namespace Artefacts.TestClient
{
	class MainClass
	{
		public const string ServiceBaseUrl = "http://localhost:8888/";

		public static void Main(string[] args)
		{
			Application.Init();
			MainWindow win = new MainWindow();
			win.Show();
			ArtefactsHost.StartHost(ServiceBaseUrl, new TextBufferWriter(win.HostTextBuffer));
			System.Threading.Thread.Sleep(1100);
			
			/* Client test code - need to figure out how to use TestFixtureAttirbute (et al) properly, with parameters etc */
			ArtefactsTestClient client = new ArtefactsTestClient(ServiceBaseUrl, new TextBufferWriter(win.ClientTextBuffer));
//			client.PutArtefact();
//			client.PutArtefactData();
//			client.PutArtefactAlternativeSerializations();
			client.PutArtefact_Disk_New();
			
			Application.Run();
			
			ArtefactsHost.StopHost();
		}
	}
}

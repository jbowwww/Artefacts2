using Artefacts;
using Artefacts.Service;
using Gtk;
using System.Threading;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using ServiceStack;
using System.Net;

/// <summary>
/// Main window.
/// </summary>
public partial class MainWindow: Gtk.Window
{	
	#region Fields & Properties
	public const string ServiceBaseUrl = "http://localhost:8888/Artefacts/";
	
	private bool _appHostThreadExit = false;

	protected ArtefactsHost AppHost = null;

	protected ServiceStack.IServiceClient Client = null;
	#endregion
	
	#region Construction & Disposal
	public MainWindow() : base (Gtk.WindowType.Toplevel)
	{
		Build();
		Maximize();
		StartHost();
		Thread.Sleep(2200);
		StartClient();
	}

	public override void Dispose()
	{
//		StopHost();
		base.Dispose();
	}
	#endregion
	
	#region Event Handlers
	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		StopHost();
		Application.Quit();
		a.RetVal = true;
	}
	
	protected void OnConfigureEvent(object sender, ConfigureEventArgs a)
	{
	}
	
	protected void OnResizeChecked(object sender, EventArgs e)
	{
		int width, height;
		GetSize(out width, out height);
		vpaned1.Position = height - 128;
	}
	#endregion

	#region Methods
	protected void StartHost()
	{
		StringBuilder sb = new StringBuilder(4096);
		new Thread(() =>  {
			AppHost = new ArtefactsHost(new StringWriter(sb));
			AppHost.Init().Start(ServiceBaseUrl);
			while (!System.Threading.Volatile.Read(ref _appHostThreadExit) || sb.Length > 0)
			{
				string s = sb.ToString();
				sb.Clear();
				tvHost.Buffer.InsertAtCursor(s);
				Thread.Sleep(868);
			}
			AppHost.Stop();
		}) {
			Priority = ThreadPriority.BelowNormal
		}.Start();
	}

	protected void StopHost()
	{
		System.Threading.Volatile.Write(ref _appHostThreadExit, true);
	}

	protected void StartClient()
	{
		//		try
		//		{
		Artefact artefact = new Artefact(new { Name = "Test", Desc = "Description" });
		byte[] artefactData = MongoDB.Bson.BsonExtensionMethods.ToBson(artefact);
		string artefactJson = MongoDB.Bson.BsonExtensionMethods.ToJson(artefact);
		string artefactJson_SS = ServiceStack.StringExtensions.ToJson(artefact);
		string artefactJsv = ServiceStack.StringExtensions.ToJsv(artefact);
		string artefactCsv = ServiceStack.StringExtensions.ToCsv(artefact);
		MongoDB.Bson.BsonDocument bsonDocument = MongoDB.Bson.BsonExtensionMethods.ToBsonDocument(artefact);
		byte[] bsonDocData = MongoDB.Bson.BsonExtensionMethods.ToBson(bsonDocument);
		//string bsonDocJson = MongoDB.Bson.BsonExtensionMethods.ToJson(bsonDocument);
//		string bsonDocJson_SS = ServiceStack.StringExtensions.ToJson(bsonDocument);
		List<object> subjects = new List<object>(new object[] {
			new object[] { artefact,					"Artefact" },
			new object[] { artefactData,				"Mongo BSON" },
			new object[] { artefactJson,				"Mongo JSON" },
			new object[] { artefactJson_SS,			"SS JSON" },
			new object[] { artefactJsv,				"SS JSV" },
			new object[] { artefactCsv,				"SS CSV" },
			new object[] { bsonDocument,				"Mongo BsonDocument" },
			new object[] { bsonDocData,				"Mongo BsonDocument -> Mongo BSON" }
			//new object[] { bsonDocJson,				"Mongo BsonDocument -> Mongo JSON" },
//			new object[] { bsonDocJson_SS,			"Mongo BsonDocument -> SS JSON" }
		});

		tvClient.Buffer.InsertAtCursor("Data:\n\t" + subjects.Select(
			o => ((string)((object[])o)[1]).PadRight(32) + (((object[])o)[0].GetType().IsArray ?
				((object[])o)[0].ToString().Replace("[]", string.Format("[{0}]", ((Array)((object[])o)[0]).Length))
		                                                :	((object[])o)[0])).Join("\n\t"));//((object[])o)[0]+ "\n\n");
		
//		                               "\n\t(Artefact)     " + artefact +
//		                       "\n\t(BsonDocument) " + bsonDocument +
//		                       "\n\t(byte[])       " + artefactData.Select(b => string.Format("{0:X2}", b)).Join("") +
//		                       "\n\t(json)         " + artefactJson +
//		                       "\n\t(SS json)      " + artefactJson_SS +
//		                       "\n\t(jsv)          " + artefactJsv +
//		                       "\n\t(csv)          " + artefactCsv +
//		                       "\n\n");
		
		Client = new ServiceStack.JsonServiceClient(ServiceBaseUrl);
		string putUrl = artefact.ToPutUrl();
		tvClient.Buffer.Insert(tvClient.Buffer.EndIter, "Client: " + Client + "\n\tPUT URL: " + putUrl + "\n\n");


		foreach (object subject in subjects.Select(o => ((object[])o)[0]))
		{
			try
			{
				tvClient.Buffer.Text += "_client.Put(" + subject + " [" + (subject == null ? "(NULL)" : subject.GetType().FullName) + "] )\n";
				object result = Client.Put(subject);
				tvClient.Buffer.Text += "\tresult = " + (result == null ? "(null)" : ((HttpWebResponse)result).ReadToEnd() + "\n");
			}
			catch (ServiceStack.WebServiceException wsex)
			{
				tvClient.Buffer.Text += wsex.ToString() + "\nServerStackTrace: " + wsex.ServerStackTrace;
				for (Exception _wsex = wsex.InnerException; _wsex != null; _wsex = _wsex.InnerException)
					tvClient.Buffer.Text += "\n" + _wsex.ToString();
				tvClient.Buffer.Text += "\n";
			}
			catch (InvalidOperationException ioex)
			{
				tvClient.Buffer.Text += ioex.ToString() + ioex.TargetSite.ToString();
				for (Exception _wsex = ioex.InnerException; _wsex != null; _wsex = _wsex.InnerException)
					tvClient.Buffer.Text += "\n" + _wsex.ToString();
				tvClient.Buffer.Text += "\n";
			}
			catch (Exception ex)
			{
				tvClient.Buffer.Text += ex.ToString();	
				for (Exception _wsex = ex.InnerException; _wsex != null; _wsex = _wsex.InnerException)	
					tvClient.Buffer.Text += "\n" + _wsex.ToString();
				tvClient.Buffer.Text += "\n";
			}
			finally
			{
				tvClient.Buffer.Text += "\n";
			}
		}
		//		}
		//		catch (Exception ex)
		//		{
		//			tvClient.Buffer.Text += ex.ToString();	
		//		}
	}
	#endregion
}

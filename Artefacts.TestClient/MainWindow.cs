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
using ServiceStack.Text;

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
		dynamic artefact = new Artefact(new { Name = "Test", Desc = "Description", testInt = 18, testBool = false });//, testObjArray = new object[] { false, 2, "three", null } });
		byte[] artefactData = MongoDB.Bson.BsonExtensionMethods.ToBson(artefact);
		string artefactJson = MongoDB.Bson.BsonExtensionMethods.ToJson(artefact);
//		string artefactJson_SS = ServiceStack.StringExtensions.ToJson(artefact);
//		string artefactJsv = ServiceStack.StringExtensions.ToJsv(artefact);
//		string artefactCsv = ServiceStack.StringExtensions.ToCsv(artefact);
		MongoDB.Bson.BsonDocument bsonDocument = MongoDB.Bson.BsonExtensionMethods.ToBsonDocument(artefact);
		byte[] bsonDocData = MongoDB.Bson.BsonExtensionMethods.ToBson(bsonDocument);
		//string bsonDocJson = MongoDB.Bson.BsonExtensionMethods.ToJson(bsonDocument);
//		string bsonDocJson_SS = ServiceStack.StringExtensions.ToJson(bsonDocument);
		List<object> subjects = new List<object>(new object[] {
			new object[] { artefact,					"Artefact" },
			new object[] { artefactData,				"Mongo BSON" },
			new object[] { artefactJson,				"Mongo JSON" },
//			new object[] { artefactJson_SS,			"SS JSON" },
//			new object[] { artefactJsv,				"SS JSV" },
//			new object[] { artefactCsv,				"SS CSV" },
			new object[] { bsonDocument,				"Mongo BsonDocument" },
			new object[] { bsonDocData,				"Mongo BsonDocument -> Mongo BSON" }
			//new object[] { bsonDocJson,				"Mongo BsonDocument -> Mongo JSON" },
//			new object[] { bsonDocJson_SS,			"Mongo BsonDocument -> SS JSON" }
		});

		tvClient.Buffer.InsertAtCursor("Data:\n\t" + subjects.Select(
			o => ((string)((object[])o)[1]).PadRight(32) + (((object[])o)[0].GetType().IsArray ?
				((object[])o)[0].ToString().Replace("[]", string.Format("[{0}]", ((Array)((object[])o)[0]).Length))
		                                                :	((object[])o)[0])).Join("\n\t") + "\n");
		Thread.Sleep(50);
		
		Client = new ServiceStack.JsonServiceClient(ServiceBaseUrl) {
			RequestFilter = (HttpWebRequest request) => tvClient.Buffer.InsertAtCursor(string.Format(
				"\nClient.{0} HTTP {6} {5} {2} bytes {1} Expect {7} Accept {8}\n",
					request.Method, request.ContentType,  request.ContentLength,
					request.UserAgent, request.MediaType, request.RequestUri,
					request.ProtocolVersion, request.Expect, request.Accept)),
			ResponseFilter = (HttpWebResponse response) => tvClient.Buffer.InsertAtCursor(string.Format(
				" --> {0} {1}: {2} {3} {5} bytes {4}: {6}\n",
				response.StatusCode, response.StatusDescription, response.CharacterSet,
				response.ContentEncoding, response.ContentType, response.ContentLength,
				response.ReadToEnd())) };

		foreach (object subject in subjects.Select(o => ((object[])o)[0]))
		{
			try
			{
				Client.Put(subject);
			}
			catch (ServiceStack.WebServiceException wsex)
			{
				tvClient.Buffer.Text += string.Format("\nError: {0}: {1}\nStatus: {2}: {3}\nResponse: {4}\n{5}: {6}\n",
					wsex.ErrorCode, wsex.ErrorMessage, wsex.StatusCode, wsex.StatusDescription, wsex.ResponseBody,
					!string.IsNullOrWhiteSpace(wsex.ServerStackTrace) ? "ServerStackTrace" : "StackTrace",
					!string.IsNullOrWhiteSpace(wsex.ServerStackTrace) ? wsex.ServerStackTrace : wsex.StackTrace);
				for (Exception _wsex = wsex.InnerException; _wsex != null; _wsex = _wsex.InnerException)
					tvClient.Buffer.Text += "\n" + _wsex.ToString();
				tvClient.Buffer.Text += "\n";
			}
			catch (InvalidOperationException ioex)
			{
				tvClient.Buffer.Text += "\n" + ioex.ToString() + ioex.TargetSite.ToString();
				for (Exception _wsex = ioex.InnerException; _wsex != null; _wsex = _wsex.InnerException)
					tvClient.Buffer.Text += "\n" + _wsex.ToString();
				tvClient.Buffer.Text += "\n";
			}
			catch (Exception ex)
			{
				tvClient.Buffer.Text += "\n" + ex.ToString();	
				for (Exception _wsex = ex.InnerException; _wsex != null; _wsex = _wsex.InnerException)	
					tvClient.Buffer.Text += "\n" + _wsex.ToString();
				tvClient.Buffer.Text += "\n";
			}
			finally
			{
				tvClient.Buffer.Text += "\n";
			}
		}
	}
	#endregion
}

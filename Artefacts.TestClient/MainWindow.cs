using ServiceStack.Text;
using Artefacts;
using Artefacts.Service;
using Gtk;
using System.Threading;
using System;
using System.IO;
using System.Text;
using System.Linq;
using ServiceStack;
using MongoDB.Bson;
using System.Collections.Generic;

public partial class MainWindow: Gtk.Window
{	
	public const string ServiceBaseUrl = "http://localhost:8888/Artefacts/";
	
	protected ArtefactsHost AppHost = null;

	protected ServiceStack.IServiceClient _client = null;
	
	private Thread _appHostThread = null;
	private bool _appHostThreadExit = false;
	private Timer _appHostUpdateTimer = null;
	
	public MainWindow(): base (Gtk.WindowType.Toplevel)
	{
		Build();
		StartHost();
		Thread.Sleep(2200);
		StartClient();
	}

	public override void Dispose()
	{
		StopHost();
		base.Dispose();
	}

	protected void StartHost()
	{
		Mutex hostOutputMutex = new Mutex();
		StringBuilder sb = new StringBuilder(4096);
		_appHostThread = new Thread(() =>  {
			//			Console.SetOut(new StringWriter(sb));
			AppHost = new ArtefactsHost(new StringWriter(sb));
			AppHost.Init().Start(ServiceBaseUrl);
			while (!_appHostThreadExit)
				Thread.Sleep(222);
			AppHost.Stop();
		});
		_appHostThread.Start();
		_appHostUpdateTimer = new Timer(state => {
			string s = sb.ToString();
			sb.Clear();
			tvHost.Buffer.Insert(tvHost.Buffer.EndIter, s);
		}, null, 800, 888);
	}

	protected void StopHost()
	{
		if (_appHostUpdateTimer != null)
		{
			_appHostUpdateTimer.Dispose();
			_appHostUpdateTimer = null;
		}
		if (_appHostThread != null)
		{
			_appHostThreadExit = true;
			while (_appHostThread.ThreadState == ThreadState.Running)
			{
			}
			_appHostThread = null;
		}
	}
	
	protected void StartClient()
	{
//		try
//		{
			Artefact artefact = new Artefact(new { Name = "Test", Desc = "Description" });
			BsonDocument bsonDocument = artefact.ToBsonDocument();
			string doc = bsonDocument.ToString();
			byte[] data = bsonDocument.ToBson();
			string json = MongoDB.Bson.BsonExtensionMethods.ToJson(artefact);
			string ss_json = ServiceStack.StringExtensions.ToJson(artefact);
//			string json3 = artefact.ToJson();
			string jsv = artefact.ToJsv();
			string csv = artefact.ToCsv();
			tvClient.Buffer.Insert(tvClient.Buffer.EndIter,
				"Data:" +
				"\n\t(Artefact)     " + artefact.ToString() +
				"\n\t(BsonDocument) " + doc +
				"\n\t(byte[])       " + data.Select(b => string.Format("\\{0:X2}", b)).Join() +
				"\n\t(json)         " + json +
				"\n\t(jsv)          " + jsv +
				"\n\t(csv)          " + csv +
				"\n\n");
			List<object> subjects = new List<object>(new object[]{ /**/artefact, bsonDocument, doc, data, ss_json });

			_client = new JsonServiceClient(ServiceBaseUrl);
			string putUrl = artefact.ToPutUrl();
			tvClient.Buffer.Insert(tvClient.Buffer.EndIter,
				"Client: " + _client.ToString() + "\n\tPUT URL: " + putUrl + "\n\n");
			
			
			foreach (object subject in subjects)
			{
				try
				{
					tvClient.Buffer.Text += "_client.Put(" + subject + " [" + (subject == null ? "(NULL)" : subject.GetType().FullName) + "] )\n";
					object result = _client.Put(subject);
					tvClient.Buffer.Text += "\tresult = " + (result == null ? "(null)" : result.ToString())/*.ReadToEnd()*/ + "\n";
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
	
	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		StopHost();
		Application.Quit();
		a.RetVal = true;
	}
	
	protected void OnConfigureEvent(object sender, ConfigureEventArgs a)
	{
		;
//		Mutex sbm = new Mutex(true);
		//MemoryStream ms = new MemoryStream(4096);
	}
}

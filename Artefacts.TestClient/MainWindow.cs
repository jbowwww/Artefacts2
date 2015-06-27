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
		StringBuilder sb = new StringBuilder(4096);
		_appHostThread = new Thread(() =>  {
			//			Console.SetOut(new StringWriter(sb));
			AppHost = new ArtefactsHost(new StringWriter(sb));
			AppHost.Init().Start("http://localhost:8888/Artefacts/");
			while (!_appHostThreadExit)
				Thread.Sleep(222);
			AppHost.Stop();
		});
		_appHostThread.Start();
		_appHostUpdateTimer = new Timer(state =>  {
			Thread.BeginCriticalRegion();
			string s = sb.ToString();
			sb.Clear();
			Thread.EndCriticalRegion();
			tvHost.Buffer.Text += s;
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
		try
		{
			_client = new JsonServiceClient("http://localhost:8888/Artefacts/");
			Artefact artefact = new Artefact(new { Name = "Test", Desc = "Description" });
			BsonDocument bsonDocument = artefact.ToBsonDocument();
			string doc = bsonDocument.ToString();
			byte[] data = bsonDocument.ToBson();
			string json = MongoDB.Bson.BsonExtensionMethods.ToJson(artefact);
			string ss_json = ServiceStack.StringExtensions.ToJson(artefact);
			tvClient.Buffer.Text +=
				"(BsonDocument) " + doc +
				"\n(byte[])     " + data.Select(b => string.Format("\\{0:X2}", b)).Join() +
				"\n(json)       " + json +
				"\n\n";
			List<object> subjects = new List<object>(new object[]{ /*artefact,*/ bsonDocument, doc, data, ss_json });
			
			foreach (object subject in subjects)
			{
				try
				{
					System.Net.HttpWebResponse result = _client.Put(subject);
					tvClient.Buffer.Text += "result = " + result == null ? "(null)" : result.ReadToEnd() + "\n\n";
				}
				catch (ServiceStack.WebServiceException wsex)
				{
					tvClient.Buffer.Text += wsex.ToString();// + wsex.ServerStackTrace;
					for (Exception _wsex = wsex; _wsex != null; _wsex = _wsex.InnerException)

						tvClient.Buffer.Text += _wsex.ToString();
				}
				catch (InvalidOperationException ioex)
				{
					tvClient.Buffer.Text += ioex.ToString() + ioex.TargetSite.ToString();	
				}
				catch (Exception ex)
				{
					tvClient.Buffer.Text += ex.ToString();	
				}
			}
		}
		catch (Exception ex)
		{
			tvClient.Buffer.Text += ex.ToString();	
		}
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

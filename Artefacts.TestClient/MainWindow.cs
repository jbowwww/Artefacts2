using Artefacts;
using Artefacts.Service;
using Gtk;
using System.Threading;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
//using ServiceStack;
using System.Net;
//using ServiceStack.Text;
using System.Configuration;

/// <summary>
/// Main window.
/// </summary>
public partial class MainWindow: Gtk.Window
{
	#region Fields & Properties
	private bool _autoScrollHost = true;
	private bool _autoScrollClient = true;
	private bool _autoScrollHostUpdated = false;
	private bool _autoScrollClientUpdated = false;
	private DateTime _autoScrollMarkHost = DateTime.Now;	
	private DateTime _autoScrollMarkClient = DateTime.Now;	
	private Timer _autoScrollTimer = null;
	public Gtk.TextBuffer HostTextBuffer { get { return tvHost.Buffer; } }
	
	public Gtk.TextBuffer ClientTextBuffer { get { return tvClient.Buffer; } }
	
	public event EventHandler OnBtnStartClicked {
		add { btnStartMain.Clicked += value; }
		remove { btnStartMain.Clicked -= value; }
	}
	
	public string DefaultTrashFolder {
		get { return btnTrashDefaultChooser.Filename; }
	}
	
	public int PostQueueCount { get; set; }

	public int DirectoryQueueCount { get; set; }

	public int CRCQueueCount { get; set; }
	#endregion
	
	#region Construction & Disposal
	public MainWindow() : base (Gtk.WindowType.Toplevel)
	{
		Build();
		btnTrashDefaultChooser.SetFilename(ConfigurationManager.AppSettings["defaultTrashPath"]);
		tvHost.Buffer.InsertText += (object o, InsertTextArgs args) => _autoScrollHostUpdated = true;
		tvClient.Buffer.InsertText += (object o, InsertTextArgs args) => _autoScrollClientUpdated = true;
		_autoScrollTimer = new Timer((o) => {
			Application.Invoke((sender, e) => {
				txtPostQueue.Text = "Post Queue: " + PostQueueCount;
				txtDirectoryQueue.Text = "Directory Queue: " + DirectoryQueueCount;
				txtCRCQueue.Text = "CRC Queue: " + CRCQueueCount;
			});
			if (_autoScrollHost && _autoScrollHostUpdated)
			{
				TextIter pos = tvHost.Buffer.EndIter;
				pos.LineOffset = 0;
				Application.Invoke((sender, e) => tvHost.ScrollToIter(pos, 0, false, 0, 0));
			}
			if (_autoScrollClient && _autoScrollClientUpdated)
			{
				TextIter pos = tvClient.Buffer.EndIter;
				pos.LineOffset = 0;
				Application.Invoke((sender, e) => tvClient.ScrollToIter(pos, 0, false, 0, 0));
			}
		}, null, 500, 500);
		Maximize();
	}

	public override void Dispose()
	{
		base.Dispose();
	}
	#endregion
	
	#region Event Handlers
	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		if (System.IO.Directory.Exists(btnTrashDefaultChooser.Filename))
		{
			ConfigurationManager.AppSettings["defaultTrashPath"] = btnTrashDefaultChooser.Filename;
			
		}
		_autoScrollTimer.Dispose();
	}
	
	protected void OnConfigureEvent(object sender, ConfigureEventArgs a)
	{
	}
	
	protected void OnResizeChecked(object sender, EventArgs e)
	{
		int width, height;
		GetSize(out width, out height);
		vpaned1.Position = height - 256;	// = vpaned1.MinPosition
//		vpaned1.MaxPosition = height - vpaned1.MinPosition;
		
		
		
	}

	protected void OnAutoScrollClient(object sender, EventArgs e)
	{
		_autoScrollClient = !_autoScrollClient;
	}

	protected void OnAutoScrollHost(object sender, EventArgs e)
	{
		_autoScrollHost = !_autoScrollHost;
	}
	#endregion

	#region Methods
	
	#endregion
}

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

/// <summary>
/// Main window.
/// </summary>
public partial class MainWindow: Gtk.Window
{	
	
	#region Fields & Properties
	private bool _autoScrollHost = true;
	private bool _autoScrollClient = true;
	private DateTime _autoScrollMarkHost = DateTime.Now;	
	private DateTime _autoScrollMarkClient = DateTime.Now;	
public Gtk.TextBuffer HostTextBuffer {
		get
		{
			return tvHost.Buffer;
		}
	}
	
	public Gtk.TextBuffer ClientTextBuffer {
		get
		{
			return tvClient.Buffer;
		}
	}
	#endregion
	
	#region Construction & Disposal
	public MainWindow() : base (Gtk.WindowType.Toplevel)
	{
		Build();
		tvHost.Buffer.InsertText += (object o, InsertTextArgs args) => 
		{
			if (_autoScrollHost && (DateTime.Now - _autoScrollMarkHost > TimeSpan.FromSeconds(1)))
			{
				_autoScrollMarkHost = DateTime.Now;
				tvHost.ScrollToIter(args.Pos, 0, false, 0, 0);
			}
		};
		tvClient.Buffer.InsertText += (object o, InsertTextArgs args) => 
		{
			if (_autoScrollClient && (DateTime.Now - _autoScrollMarkClient > TimeSpan.FromSeconds(1)))
			{
				_autoScrollMarkClient = DateTime.Now;
				tvClient.ScrollToIter(args.Pos, 0, false, 0, 0);
			}
		};
//		tvHost.InsertAtCursor += (object o, InsertAtCursorArgs args) =>
//		{
//			tvHost.ScrollToIter(tvHost.Buffer.EndIter, 0, false, 0, 0);
//		};
//		tvClient.InsertAtCursor += (object o, InsertAtCursorArgs args) => 
//		{
//			tvClient.ScrollToIter(tvClient.Buffer.EndIter, 0, false, 0, 0);
//		};
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

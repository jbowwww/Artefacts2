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
	#endregion

	#region Methods
	
	#endregion
}

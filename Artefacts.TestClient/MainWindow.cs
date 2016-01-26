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
using System.Reflection;

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
	TextIter posHost;
	TextIter posClient;
	
	public event EventHandler OnBtnStartClicked {
		add { btnStartMain.Clicked += value; }
		remove { btnStartMain.Clicked -= value; }
	}
	
	public string DefaultTrashFolder {
		get { return btnTrashDefaultChooser.Filename; }
	}
	
	public delegate int GetIntDelegate();
	public delegate string GetStringDelegate();
	public delegate MethodInfo GetMethodDelegate();
	public delegate TimeSpan GetTimeSpanDelegate();
	public delegate DateTime GetDateTimeDelegate();
	public GetIntDelegate GetPostQueueCount;
	public GetIntDelegate GetDirQueueCount;
	public GetIntDelegate GetCRCQueueCount;
	public GetMethodDelegate GetCurrentTest;
	public GetDateTimeDelegate GetCurrentTestTime;
	public bool EnableStatusGetters {
		get { return _autoScrollTimer != null; }
		set
		{
			if (value)
				_autoScrollTimer = new Timer(OnUpdateStatuses, null, 500, 500);
			else
			{
				if (_autoScrollTimer != null)
				{
					using (WaitHandle waitTimerStop = new EventWaitHandle(false, EventResetMode.ManualReset))
					{
						_autoScrollTimer.Dispose(waitTimerStop);
						waitTimerStop.WaitOne();
					}	
					_autoScrollTimer = null;
				}
				OnUpdateStatuses(null);
				txtPostQueue.Text = "Post Queue: 0";
				txtDirectoryQueue.Text = "Directory Queue: 0";
				txtCRCQueue.Text = "CRC Queue: 0";
				txtTestName.Text = "Current Test: (not running)";
				//txtTestTime.Text = GetCurrentTestTime == null ? "" : (DateTime.Now - GetCurrentTestTime.Invoke()).ToString();
			}
		}
	}
	
//	public int PostQueueCount { get; set; }
//	public int DirectoryQueueCount { get; set; }
//	public int CRCQueueCount { get; set; }
	#endregion
	
	#region Construction & Disposal
	public MainWindow() : base (Gtk.WindowType.Toplevel)
	{
		Build();
		btnTrashDefaultChooser.SetFilename(ConfigurationManager.AppSettings["defaultTrashPath"]);
		tvHost.Buffer.InsertText += (object o, InsertTextArgs args) => _autoScrollHostUpdated = true;
		tvClient.Buffer.InsertText += (object o, InsertTextArgs args) => _autoScrollClientUpdated = true;
		EnableStatusGetters = true;
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
		
	protected void OnUpdateStatuses(object state)
	{
		Application.Invoke((sender, e) => {
			txtPostQueue.Text = "Post Queue: " + (GetPostQueueCount == null ? 0 : GetPostQueueCount.Invoke());
			txtDirectoryQueue.Text = "Directory Queue: " + (GetDirQueueCount == null ? 0 : GetDirQueueCount.Invoke());
			txtCRCQueue.Text = "CRC Queue: " + (GetCRCQueueCount == null ? 0 : GetCRCQueueCount.Invoke());
			MethodInfo currentTest = GetCurrentTest == null ? null : GetCurrentTest.Invoke();
			txtTestName.Text = currentTest == null ? "No test running" :
				"Current Test: " + currentTest.DeclaringType.FullName + "." + currentTest.Name;
			txtTestTime.Text = GetCurrentTestTime == null ? "" :
				(DateTime.Now - GetCurrentTestTime.Invoke()).ToString();
		});
		if (_autoScrollHost && _autoScrollHostUpdated)
		{
			TextIter pos = tvHost.Buffer.EndIter;
			if (posHost.Equal(default(TextIter)) || !pos.Equal(posHost))	//.Offset != posHost.Offset)
			{
				posHost = pos;
				pos.LineOffset = 0;
				Application.Invoke((sender, e) => tvHost.ScrollToIter(pos, 0, false, 0, 0));
			}
		}
		if (_autoScrollClient && _autoScrollClientUpdated)
		{
			TextIter pos = tvClient.Buffer.EndIter;
			if (posClient.Equal(default(TextIter)) || !pos.Equal(posClient))
			{
				posClient = pos;
				pos.LineOffset = 0;
				Application.Invoke((sender, e) => tvClient.ScrollToIter(pos, 0, false, 0, 0));
			}
		}
	}
	#endregion

	#region Methods
	
	#endregion
}

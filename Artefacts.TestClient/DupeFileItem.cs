using System;
using System.Diagnostics;

namespace Artefacts.TestClient
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class DupeFileItem : Gtk.Bin
	{
		private DupeProcessWindow _processWindow;
		
		public bool Checked {
			get { return chkDupeSelect.Active; }	// chkDupeSelect.State == Gtk.StateType.Active; }
			set { chkDupeSelect.Active = value; }
		}
		
		public string Path {
			get { return txtDupePath.Text; }
			set { txtDupePath.Text = value; }
		}
		
		public DupeFileItem(string dupePath, DupeProcessWindow processWindow)
		{
			this.Build();
			_processWindow = processWindow;
			txtDupePath.Text = dupePath;
			btnDupeKeep.Clicked += (object sender, EventArgs e) => {
				_processWindow.DupePrimaryPath = Path;
				_processWindow.CheckAllExceptPrimary();
			};
			btnDupeOpenFolder.Clicked += (object sender, EventArgs e) => {
				Process.Start("nautilus", dupePath);
			};
		}
	}
}


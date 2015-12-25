using System;
using System.Collections.Generic;
using Artefacts.FileSystem;
using System.Diagnostics;

namespace Artefacts.TestClient
{
	public partial class DupeProcessWindow : Gtk.Window
	{
		public static string _moveDupeToPath = "/mnt/Trapdoor/trash";
		
		public string DupePrimaryPath {
			get { return txtFilename.Text; }
			set { txtFilename.Text = value; }
		}
		
		public DupeProcessWindow(IEnumerable<File> dupeFiles) : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build();
			foreach (File file in dupeFiles)
				viewDupes.Add(new DupeFileItem(file.Path, this));
			ShowAll();
//			viewDupes.Children[0].Activate();
		}

		public void CheckAllExceptPrimary()
		{
			foreach (Gtk.Widget dupeEntry in viewDupes.Children)
			{
				DupeFileItem dupeFileItem = (DupeFileItem)dupeEntry;
				if (dupeFileItem.Path != txtFilename.Text)
					dupeFileItem.Checked = true;
			}
		}
		
		protected void MoveFileToTrash(string path)
		{
			Process.Start("mv", path + " " + _moveDupeToPath);
		}
		
		protected override bool OnConfigureEvent(Gdk.EventConfigure evnt)
		{
			
			return base.OnConfigureEvent(evnt);
		}
		
		protected void OnSelectAllActionActivated(object sender, EventArgs e)
		{
			CheckAllExceptPrimary();
		}

		protected void OnSelectNoneActionActivated(object sender, EventArgs e)
		{
			foreach (Gtk.Widget dupeEntry in viewDupes.Children)
			{
				DupeFileItem dupeFileItem = (DupeFileItem)dupeEntry;
				dupeFileItem.Checked = false;
			}
		}

		protected void OnInvertSelectionActionActivated(object sender, EventArgs e)
		{
			foreach (Gtk.Widget dupeEntry in viewDupes.Children)
			{
				DupeFileItem dupeFileItem = (DupeFileItem)dupeEntry;
				dupeFileItem.Checked = !dupeFileItem.Checked;
			}}


		protected void OnSaveActionActivated(object sender, EventArgs e)
		{
			foreach (Gtk.Widget dupeEntry in viewDupes.Children)
			{
				DupeFileItem dupeFileItem = (DupeFileItem)dupeEntry;
				if (dupeFileItem.Checked && (dupeFileItem.Path.CompareIgnoreCase(DupePrimaryPath) != 0))
				{
					MoveFileToTrash(dupeFileItem.Path);	
				}
			}
		}
	}
}


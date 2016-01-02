using System;
using System.Collections.Generic;
using Artefacts.FileSystem;
using System.Diagnostics;
using Gtk;
using Artefacts.Extensions;

namespace Artefacts.TestClient
{
	public partial class DupeProcessWindow : Gtk.Window
	{
		public static string _moveDupeToPath = "/mnt/Trapdoor/trash";
	
		private Gtk.TreeStore _model;
	
		enum Column {
			Name,
			Location,
			Size,
			CRC,
			Created,
			Accessed,
			Modified
		};
		
		public DupeProcessWindow(IEnumerable<File> dupeFiles) : 
				base(Gtk.WindowType.Toplevel)
		{
			this.Build();
			
			PopulateModel(dupeFiles);
			PopulateColumns();
			
			ShowAll();
		}

		private void PopulateColumns()
		{
			CellRendererText rendererText;
			TreeViewColumn column;
			
			rendererText = new CellRendererText();
			column = new TreeViewColumn("Name", rendererText, "text", Column.Name);
			column.SortColumnId = (int)Column.Name;
			viewDupes.AppendColumn(column);
			
			rendererText = new CellRendererText();
			column = new TreeViewColumn("Location", rendererText, "text", Column.Location);
			column.SortColumnId = (int)Column.Location;
			viewDupes.AppendColumn(column);

			rendererText = new CellRendererText();
			column = new TreeViewColumn("Size", rendererText, "text", Column.Size);
			column.SortColumnId = (int)Column.Size;
			viewDupes.AppendColumn(column);

			rendererText = new CellRendererText();
			column = new TreeViewColumn("CRC", rendererText, "text", Column.CRC);
			column.SortColumnId = (int)Column.CRC;
			viewDupes.AppendColumn(column);

			rendererText = new CellRendererText();
			column = new TreeViewColumn("Created", rendererText, "text", Column.Created);
			column.SortColumnId = (int)Column.Created;
			viewDupes.AppendColumn(column);

			rendererText = new CellRendererText();
			column = new TreeViewColumn("Accessed", rendererText, "text", Column.Accessed);
			column.SortColumnId = (int)Column.Accessed;
			viewDupes.AppendColumn(column);

			rendererText = new CellRendererText();
			column = new TreeViewColumn("Modified", rendererText, "text", Column.Modified);
			column.SortColumnId = (int)Column.Modified;
			viewDupes.AppendColumn(column);


		}

		private void PopulateModel(IEnumerable<File> files)
		{
			viewDupes.Model = _model = 
				new Gtk.TreeStore(
					typeof(string),			// Name (no path)
					typeof(string),			// Location (path, no name)
					
					typeof(string),			// Name (no path)
					typeof(string),			// Location (path, no name)
					typeof(string),			// Name (no path)
					typeof(string),			// Location (path, no name)
					typeof(string)			// Name (no path)
//					typeof(long),			// Size
//					typeof(long),			// CRC
//					typeof(DateTime),		// Created
//					typeof(DateTime),		// Accessed
//					typeof(DateTime)		// Modified
					);
			foreach (File file in files)
				_model.AppendValues(
					file.Name, file.DirectoryPath ?? "", file.Size.ToString(), file.CRC.Value.ToHex(),
					file.CreationTime.ToString(), file.LastAccessTime.ToString(), file.LastWriteTime.ToString());
		}
		
		public void CheckAllExceptPrimary()
		{
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
		}

		protected void OnInvertSelectionActionActivated(object sender, EventArgs e)
		{
			
		}


		protected void OnSaveActionActivated(object sender, EventArgs e)
		{
		}
	}
}


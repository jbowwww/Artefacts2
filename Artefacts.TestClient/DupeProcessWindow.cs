using System;
using System.Collections.Generic;
using Artefacts.FileSystem;
using System.Diagnostics;
using Gtk;
using Artefacts.Extensions;
using System.Configuration;

namespace Artefacts.TestClient
{
	public partial class DupeProcessWindow : Gtk.Window
	{
		public static string _moveDupeToPath = "/mnt/Trapdoor/trash";
	
		private readonly TextBufferWriter _debug;
		
		private IList<IList<File>> _fileDupeGroups;
		private int _fileDupeGroupIndex = 0;
		
		private Gtk.TreeStore _model;
	
		enum Column {
			Instance,
			Select,
			Name,
			Path,
			Size,
			CRC,
			Created,
			Accessed,
			Modified
		};
		
		public DupeProcessWindow(IList<IList<File>> dupeFiles, string trashFolder, TextBufferWriter debug) : 
				base(Gtk.WindowType.Toplevel)
		{
			_debug = debug;
			_fileDupeGroups = dupeFiles;
			this.Build();
			
			btnChooseTrashDir.SetFilename(trashFolder ?? ConfigurationManager.AppSettings["defaultTrashPath"]);
			PopulateModel(dupeFiles[0]);
			PopulateColumns();
			
			ShowAll();
		}

		private void PopulateColumns()
		{
			CellRendererText rendererText;
			TreeViewColumn column;
			
			CellRendererToggle rendererToggle = new CellRendererToggle();
			rendererToggle.Toggled += (object o, ToggledArgs args) => {
				TreeIter iter;
				if (_model.GetIter(out iter, new TreePath(args.Path)))
				{
					_model.SetValue(iter, (int)Column.Select, !(bool)_model.GetValue(iter, (int)Column.Select));
				}
			};
			column = new TreeViewColumn("", rendererToggle, "active", Column.Select);
			column.SortColumnId = (int)Column.Select;
			viewDupes.AppendColumn(column);
			
			rendererText = new CellRendererText();
			column = new TreeViewColumn("Name", rendererText, "text", Column.Name);
			column.SortColumnId = (int)Column.Name;
			viewDupes.AppendColumn(column);
			
			rendererText = new CellRendererText();
			column = new TreeViewColumn("Location", rendererText, "text", Column.Path);
			column.SortColumnId = (int)Column.Path;
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
					typeof(object),
					typeof(bool),
					typeof(string),			// Name (no path)
					typeof(string),			// Location (path, no name)
					
//					typeof(string),			// Name (no path)
//					typeof(string),			// Location (path, no name)
//					typeof(string),			// Name (no path)
//					typeof(string),			// Location (path, no name)
//					typeof(string)			// Name (no path)
					typeof(long),			// Size
					typeof(long),			// CRC
					typeof(string),//DateTime),		// Created
					typeof(string),//DateTime),		// Accessed
					typeof(string)//DateTime)		// Modified
					);
			foreach (File file in files)
				_model.AppendValues(
					file,
					false,
					file.Name,
					file.Path,	//file.DirectoryPath ?? "",
					file.Size,//.ToString(),
					file.CRC.Value,//.ToHex(),
					file.CreationTime.ToString(),
					file.LastAccessTime.ToString(),
					file.LastWriteTime.ToString()
				);
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

		protected void OnViewDupesSelectCursorRow(object o, SelectCursorRowArgs args)
		{
			TreePath[] selected = viewDupes.Selection.GetSelectedRows();
			long totalSize = 0;
			foreach (TreePath path in selected)
			{
				TreeIter iter;
				if (_model.GetIter(out iter, path))
				{
					long size = (long)_model.GetValue(iter, (int)Column.Size);
					totalSize += size;
				}
			}
		}

		protected void OnBtnMoveTrashClicked(object sender, EventArgs clickedArgs)
		{
//			uint trashPathLength;
			string trashPath;
//			string trashPathReversed;
			trashPath = btnChooseTrashDir.Filename;//.Path(out trashPathLength, out trashPath, out trashPathReversed);
			if (!string.IsNullOrWhiteSpace(trashPath))		//trashPathLength > 0)
			{
//				using (System.IO.FileStream outFile = System.IO.File.OpenWrite("trash.sh"))
//				{
//				using (System.IO.FileStream fsLog = System.IO.File.OpenWrite("fsLog.txt"))
//				{
					TreePath[] selected = viewDupes.Selection.GetSelectedRows();
					long totalSize = 0;
					foreach (TreePath path in selected)
					{
						TreeIter iter;
						if (_model.GetIter(out iter, path))
						{
							File file = (File)_model.GetValue(iter, (int)Column.Instance);
							string filePath = file.Path;// (string)_model.GetValue(iter, (int)Column.Path);
							long size = file.Size;//(long)_model.GetValue(iter, (int)Column.Size);
							totalSize += size;
							//string cmd = "mv \"" + filePath + "\" \"" + trashPath + "\"\n";
							_debug.Write("Moving \"" + filePath + "\" (" + File.FormatSize(size) + ") to trash folder \"" + trashPath + "\": ");
							using (Process mvProc = Process.Start("mv", "\"" + filePath + "\" \"" + trashPath + "\""))
							{
								mvProc.OutputDataReceived +=
									(object s, DataReceivedEventArgs e) => {
										_debug.Write(e.Data);
//									byte[] fsBuf = System.Text.Encoding.Default.GetBytes(e.Data);
//										fsLog.Write(fsBuf, 0, fsBuf.Length);
									};
//								while (!mvProc.HasExited || !mvProc.StandardOutput.EndOfStream)
//								{
//									string line = mvProc.StandardOutput.ReadLine();
//									_debug.WriteLine(line);
//								}
								
								while (!mvProc.WaitForExit(500) && !mvProc.HasExited)
									;
								if (mvProc.ExitCode != 0)
								{
									_debug.WriteLine("\tERROR! ExitCode = " + mvProc.ExitCode);
								}
								else
								{
//									file 
								}
							}
//							byte[] buf = System.Text.Encoding.Default.GetBytes(cmd);
//							outFile.Write(buf, 0, buf.Length);
						}
					}
					_debug.WriteLine("Total " + File.FormatSize(totalSize) + " moved\n");
				}
//				}
//			}
		}

		protected void OnBtnDupeGroupDismissClicked(object sender, EventArgs e)
		{
			PopulateModel(_fileDupeGroups[_fileDupeGroupIndex++]);
			ShowAll();
		}

	}
}


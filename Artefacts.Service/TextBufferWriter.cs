//using System;
using System.IO;
using Gtk;
using GLib;
using Gtk.DotNet;
//using System.Threading;
//using System;

namespace Artefacts
{
	public class TextBufferWriter : TextWriter
	{
		private object _sync;
		
		private TextBuffer _textBuffer;
		
		private object _sender;
		
		public TextBufferWriter(TextBuffer textBuffer, object sender)
		{
			_sync = textBuffer;
			_textBuffer = textBuffer;
			_sender = sender;
//			base.Write(
//			);
//			base.NewLine = "\r\n";
		}
		
		public override System.Text.Encoding Encoding {
			get
			{
				return System.Text.Encoding.Default;
			}
		}
		
		public override void Write(char value)
		{
			//				Thread.BeginCriticalRegion();
//				InsertTextArgs e = new InsertTextArgs();
			Gtk.Application.Invoke(delegate {
				lock (_sync)
				{
					TextIter iter = _textBuffer.EndIter;
					_textBuffer.Insert(ref iter, value.ToString());
					
				}
				});
//				_textBuffer.InsertText(_sender, e);
//					(_sender, (System.EventArgs)(new InsertTextArgs() {
//
//				}), new System.EventHandler(
//				new System.EventHandler(_textBuffer.InsertText).Invoke(_sender, e);
//				_textBuffer.InsertText.Invoke(_sender, );
//			_textBuffer.BeginUserAction();
//			_textBuffer.InsertAtCursor(value.ToString());
//			_textBuffer.EndUserAction();
//				Thread.EndCriticalRegion();
		}
		
		public override void Write(char[] buffer, int index, int count)
		{
			
//			Thread.BeginCriticalRegion();
//			_textBuffer.BeginUserAction();
			Gtk.Application.Invoke(delegate {
				lock (_sync)
				{
					TextIter iter = _textBuffer.EndIter;
					_textBuffer.Insert(ref iter, buffer.ToString().Substring(index, count));
				}
			});
//			_textBuffer.EndUserAction();
//				Thread.EndCriticalRegion();
		}
		
		public override void Write(string value)
		{
			//			Thread.BeginCriticalRegion();
//			_textBuffer.BeginUserAction();
				Gtk.Application.Invoke(delegate {
				lock (_sync)
				{
					TextIter iter = _textBuffer.EndIter;
					_textBuffer.Insert(ref iter, value);
				}
				});
//			_textBuffer.EndUserAction();
//				Thread.EndCriticalRegion();
		}
		
		public override void WriteLine()
		{
			
//			Thread.BeginCriticalRegion();
//			_textBuffer.BeginUserAction();
				Gtk.Application.Invoke(delegate {
				lock (_sync)
				{
					TextIter iter = _textBuffer.EndIter;
				_textBuffer.Insert(ref iter, "\n");
				}
				});
//			_textBuffer.EndUserAction();
//				Thread.EndCriticalRegion();
		}
	}
}


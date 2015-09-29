using System;
using System.IO;
using Gtk;

namespace Artefacts
{
	public class TextBufferWriter : TextWriter
	{
		private object _sync = new object();
		
		private TextBuffer _textBuffer;
		
		public TextBufferWriter(TextBuffer textBuffer)
		{
			_textBuffer = textBuffer;
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
			lock (_sync)
			{
				_textBuffer.InsertAtCursor(value.ToString());
			}
		}
		
		public override void Write(char[] buffer, int index, int count)
		{
			lock (_sync)
			{
				_textBuffer.InsertAtCursor(buffer.ToString().Substring(index, count));
			}
		}
		
		public override void Write(string value)
		{
			lock (_sync)
			{
				_textBuffer.InsertAtCursor(value);
			}
		}
		
		public override void WriteLine()
		{
			lock (_sync)
			{
				_textBuffer.InsertAtCursor("\n");
			}
		}
	}
}


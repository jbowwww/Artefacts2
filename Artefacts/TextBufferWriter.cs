using System;
using System.IO;
using Gtk;

namespace Artefacts
{
	public class TextBufferWriter : TextWriter
	{
		private TextBuffer _textBuffer;
		
		public TextBufferWriter(TextBuffer textBuffer)
		{
			_textBuffer = textBuffer;
		}
		
		public override System.Text.Encoding Encoding {
			get
			{
				return System.Text.Encoding.Default;
			}
		}
		
		public override void Write(char value)
		{
			_textBuffer.InsertAtCursor(value.ToString());
		}
		
		public override void Write(char[] buffer, int index, int count)
		{
			_textBuffer.InsertAtCursor(buffer.ToString().Substring(index, count));
		}
		
		public override void Write(string value)
		{
			_textBuffer.InsertAtCursor(value);
		}
	}
}


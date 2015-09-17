using System;
using System.IO;
using Gtk;

namespace Artefacts
{
	public class TextBufferWriter : TextWriter
	{
		internal class GroupContext : IDisposable
		{
			internal GroupContext()
			{
				lock (_sync)
				{
					_indentLevel++;
				}
			}
			
			void IDisposable.Dispose()
			{
				lock (_sync)
				{
					if (--_indentLevel < 0)
						_indentLevel = 0;
				}
			}
		}
		
		private object _sync = new object();
		
		private TextBuffer _textBuffer;
	
		private int _indentLevel = 0;
		private string _indent = "  ";
		private string _indentedItem = "+ ";
		
		public TextBufferWriter(TextBuffer textBuffer)
		{
			_textBuffer = textBuffer;
//			base.NewLine = "\r\n";
		}
		
		public override System.Text.Encoding Encoding {
			get { return System.Text.Encoding.Default; }
		}
		
		private string Indent(string value)
		{	// would using <= instead of == be useful? ie useful to hgave negative indent level to represent somethinbg?
			return string.Concat(
				_indentLevel <= 0
				?	string.Empty
				:	_indentLevel == 1
					?	_indentedItem
					:	string.Concat(_indent.Repeat(_indentLevel - 1), _indentedItem),
				value);
		}
		
		public override void Write(char value)
		{
			lock (_sync)
			{
				_textBuffer.InsertAtCursor(Indent(value.ToString()));
			}
		}
		
		public override void Write(char[] buffer, int index, int count)
		{
			lock (_sync)
			{
				_textBuffer.InsertAtCursor(Indent(buffer.ToString().Substring(index, count)));
			}
		}
		
		public override void Write(string value)
		{
			lock (_sync)
			{
				_textBuffer.InsertAtCursor(Indent(value));
			}
		}
		
		public override void WriteLine()
		{
			lock (_sync)
			{
				_textBuffer.InsertAtCursor(Indent("\n"));
			}
		}
		
		public IDisposable StartGroup(string msg = string.Empty)
		{
//			Write("+ ");
			if (msg != null)
				WriteLine(string.Concat("-- ", msg));
			return new GroupContext();
		}
	}
}


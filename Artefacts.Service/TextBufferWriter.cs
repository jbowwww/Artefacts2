//using System;
using System.IO;
using Gtk;
using GLib;
using Gtk.DotNet;
//using System.Threading;
//using System;
using System;
using ServiceStack.Logging;

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
				lock (_sync)
				{
				Gtk.Application.Invoke(delegate {
		TextIter iter = _textBuffer.EndIter;
					_textBuffer.Insert(ref iter, value.ToString());
					
				});
			}
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
				lock (_sync)
				{
				Gtk.Application.Invoke(delegate {
		TextIter iter = _textBuffer.EndIter;
					_textBuffer.Insert(ref iter, buffer.ToString().Substring(index, count));
				});
			}
//			_textBuffer.EndUserAction();
//				Thread.EndCriticalRegion();
		}
		
		public override void Write(string value)
		{
			string timeStamp = string.Concat(DateTime.Now.ToShortTimeString(), " ");
			value = string.Concat(timeStamp, value.Replace("\n", string.Concat("\n", timeStamp)));
				lock (_sync)
				{
				Gtk.Application.Invoke(delegate {
		TextIter iter = _textBuffer.EndIter;
					_textBuffer.Insert(ref iter, value);
				});
			}
		}
		
		public override void WriteLine()
		{
				lock (_sync)
				{
				Gtk.Application.Invoke(delegate {
		TextIter iter = _textBuffer.EndIter;
					_textBuffer.Insert(ref iter, "\n");
				});
				}
		}
		
		public Log GetLog(string sourceName)
		{
			return new Log(sourceName);
		}

		public class Log : ILog
		{
			private string _prefixDebug;
			private string _prefixError;
			private string _prefixInfo;
			private string _prefixFatal;
			private string _prefixWarn;

			public string SourceName { get; private set; }
			public TextBufferWriter Writer { get; private set; }
			public Log(TextBufferWriter writer, string sourceName)
			{
				Writer = writer;
				SourceName = sourceName;
				_prefixDebug = string.Concat("DEBUG: ", sourceName, ": ");
				_prefixError = string.Concat("ERROR: ", sourceName, ": ");
				_prefixInfo = string.Concat("INFO: ", sourceName, ": ");
				_prefixFatal = string.Concat("FATAL: ", sourceName, ": ");
				_prefixWarn = string.Concat("WARN: ", sourceName, ": ");
			}
			
			private string WriteFormatted(string prefix, string message)
			{
				Writer.WriteLine(string.Concat(prefix, message.Replace("\n", string.Concat("\n", prefix))));
			}
			
			public void Debug(object message)
			{
				WriteFormatted(_prefixDebug, message.ToString());
			}

			public void Debug(object message, Exception exception)
			{
				WriteFormatted(_prefixDebug, message.ToString() + ": " + exception.Format());
			}
	
			public void DebugFormat(string format, params object[] args)
			{
				WriteFormatted(_prefixDebug, string.Format(format, args));
			}
	
			public void Error(object message)
			{
				WriteFormatted(_prefixError, message.ToString());
			}
	
			public void Error(object message, Exception exception)
			{
				WriteFormatted(_prefixError, message.ToString() + ": " + exception.Format());
			}
	
			public void ErrorFormat(string format, params object[] args)
			{
				WriteFormatted(_prefixError, string.Format(format, args));
			}
	
			public void Fatal(object message)
			{
				WriteFormatted(_prefixFatal, message.ToString());
			}
	
			public void Fatal(object message, Exception exception)
			{
				WriteFormatted(_prefixFatal, message.ToString() + ": " + exception.Format());
			}
	
			public void FatalFormat(string format, params object[] args)
			{
				WriteFormatted(_prefixFatal, string.Format(format, args));
			}
	
			public void Info(object message)
			{
				WriteFormatted(_prefixInfo, message.ToString());
			}
	
			public void Info(object message, Exception exception)
			{
				WriteFormatted(_prefixInfo, message.ToString() + ": " + exception.Format());
			}
	
			public void InfoFormat(string format, params object[] args)
			{
				WriteFormatted(_prefixInfo, string.Format(format, args));
			}
	
			public void Warn(object message)
			{
				WriteFormatted(_prefixWarn, message.ToString());
			}
	
			public void Warn(object message, Exception exception)
			{
				WriteFormatted(_prefixWarn, message.ToString() + ": " + exception.Format());
			}
	
			public void WarnFormat(string format, params object[] args)
			{
				WriteFormatted(_prefixWarn, string.Format(format, args));
			}
	
			public bool IsDebugEnabled {
				get {
					return true;
				}
			}

		#endregion
	}
}


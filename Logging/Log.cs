using System;
using ServiceStack.Logging;
using System.Collections.Generic;
using ServiceStack.Support;

namespace Logging
{
	public class Log : ILogFactory
	{
		enum Level
		{
			Debug,
			Info,
			Warn,
			Error,
			Fatal
		};
		
		class Logger : ILog
		{
			class Entry
			{
				
			}
			
			public readonly Dictionary<Level, IList<object>> Logs = new Dictionary<Level, IList<object>>();
			public readonly string Name;

			public Logger(string name)
			{
				if (name == null)
					throw new ArgumentNullException("name");
				Name = name;
				
			}
			
			#region ILog implementation
			public void Debug(object message)
			{
				throw new NotImplementedException();
			}
			public void Debug(object message, Exception exception)
			{
				throw new NotImplementedException();
			}
			public void DebugFormat(string format, params object[] args)
			{
				throw new NotImplementedException();
			}
			public void Error(object message)
			{
				throw new NotImplementedException();
			}
			public void Error(object message, Exception exception)
			{
				throw new NotImplementedException();
			}
			public void ErrorFormat(string format, params object[] args)
			{
				throw new NotImplementedException();
			}
			public void Fatal(object message)
			{
				throw new NotImplementedException();
			}
			public void Fatal(object message, Exception exception)
			{
				throw new NotImplementedException();
			}
			public void FatalFormat(string format, params object[] args)
			{
				throw new NotImplementedException();
			}
			public void Info(object message)
			{
				throw new NotImplementedException();
			}
			public void Info(object message, Exception exception)
			{
				throw new NotImplementedException();
			}
			public void InfoFormat(string format, params object[] args)
			{
				throw new NotImplementedException();
			}
			public void Warn(object message)
			{
				throw new NotImplementedException();
			}
			public void Warn(object message, Exception exception)
			{
				throw new NotImplementedException();
			}
			public void WarnFormat(string format, params object[] args)
			{
				throw new NotImplementedException();
			}
			public bool IsDebugEnabled {
				get {
					throw new NotImplementedException();
				}
			}
			#endregion
			
		}
		
		public readonly List<ILog> Outputs = new List<ILog>();
		
		public Log()
		{
		}

		#region ILogFactory implementation

		public ILog GetLogger(Type type)
		{
			throw new NotImplementedException();
		}

		public ILog GetLogger(string typeName)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}


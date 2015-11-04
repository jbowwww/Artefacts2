using System;
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Logging;
using System.Linq;
using NUnit.Framework;
using System.IO;

namespace Artefacts.Service
{
	public abstract class TestClientBase : IDisposable
	{
		public static readonly ILog Log;
		
		private TextWriter _writer;
		
		static TestClientBase()
		{
			Log = ArtefactsClient.LogFactory.GetLogger(typeof(TestClientBase));
		}
		
		public TestClientBase(TextWriter writer = null)
		{
			_writer = writer ?? TextWriter.Null;
		}

		/// <summary>
		/// Releases all resource used by the <see cref="Artefacts.Service.TestClientBase"/> object.
		/// </summary>
		/// <remarks>
		/// IDisposable implementation
		/// 
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="Artefacts.Service.TestClientBase"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Artefacts.Service.TestClientBase"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="Artefacts.Service.TestClientBase"/>
		/// so the garbage collector can reclaim the memory that the <see cref="Artefacts.Service.TestClientBase"/> was occupying.
		/// </remarks>
		public void Dispose()
		{
			
		}
		
		public void Run()
		{
			Type thisType = this.GetType();
			IEnumerable<MethodInfo> testMethods = thisType.GetMethods()
				.Where(mi => mi.GetCustomAttribute<TestAttribute>() != null && EnableTest(mi));
			foreach (MethodInfo mi in testMethods)
			{
				string testName = string.Concat(mi.DeclaringType.FullName, ".", mi.Name);
				try
				{
					Log.InfoFormat("\n--------\nTest: {0}\n--------", testName);
					_writer.WriteLine("\n--------\nTest: {0}\n--------", testName);
					mi.Invoke(this, new object[] { });
				}
				//					catch (WebServiceException e) { clientWriter.WriteLine("ERROR: " + ex.Format()); }
				//					catch (TargetInvocationException e) { clientWriter.WriteLine("ERROR: " + ex.Format()); }
				catch (Exception ex) {
					Log.Error("! ERROR: ", ex);
					_writer.WriteLine("! ERROR: " + ex.Format());
				}
				finally
				{
					Log.InfoFormat("--------\nFinished: {0}\n--------", testName);
					_writer.WriteLine("--------\nFinished: {0}\n--------", testName);
				}
			}
		}
			
		public virtual bool EnableTest(MethodInfo method)
		{
			return true;	
		}
	}
}


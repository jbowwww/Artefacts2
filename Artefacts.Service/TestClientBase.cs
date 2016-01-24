using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using ServiceStack.Logging;
using NUnit.Framework;

namespace Artefacts.Service
{
	public abstract class TestClientBase : IDisposable
	{
		public static readonly ILog Log;
		
		private TextWriter _writer;
		
		public static MethodInfo CurrentTest { get; protected set; }
		public static DateTime CurrentTestStartTime { get; protected set; }
		
		public static readonly List<TimeSpan> TestTimes = new List<TimeSpan>();
		public static readonly List<MethodInfo> Tests = new List<MethodInfo>();
		
		static TestClientBase()
		{
			Log = Artefact.LogFactory.GetLogger(typeof(TestClientBase));
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
		public virtual void Dispose()
		{
			
		}
		
		public void Run()
		{
			Type thisType = this.GetType();
			IEnumerable<MethodInfo> testMethods = thisType.GetMethods()
				.Where(mi => mi.GetCustomAttribute<TestAttribute>() != null && EnableTest(mi));
			foreach (MethodInfo mi in testMethods)
			{
				CurrentTestStartTime = DateTime.Now;
				Tests.Add(CurrentTest = mi);
				DateTime T1 = DateTime.Now;
				string testName = string.Concat(mi.DeclaringType.FullName, ".", mi.Name);
				try
				{
					
					Log.InfoFormat("\n--------Test: {0}-------- {1}", testName, T1);
					_writer.WriteLine("\n--------Test: {0}-------- {1}", testName, T1);
					mi.Invoke(this, new object[] { });
				}
//									catch (WebServiceException e) { clientWriter.WriteLine("ERROR: " + ex.Format()); }
//									catch (TargetInvocationException e) { clientWriter.WriteLine("ERROR: " + ex.Format()); }
				catch (Exception ex) {
					DateTime Te = DateTime.Now;
					Log.ErrorFormat("\n--!-- !- ERROR: {0}    {1}\n{2}", ex.ToString(), Te, ex.Format());
					_writer.WriteLine(string.Format("\n--!-- !- ERROR: {0}    {1}\n{2}", ex.ToString(), Te, ex.Format()));
				}
				finally
				{
					TestTimes.Add(DateTime.Now - CurrentTestStartTime);
					CurrentTestStartTime = default(DateTime);
					CurrentTest = null;
					DateTime T2 = DateTime.Now;
					TimeSpan Td = T2 - T1;
					Log.InfoFormat("\\n--------Finished: {0}-------- {1} Td={2}", testName, T2, Td);
					_writer.WriteLine("\n--------Finished: {0}-------- {1} Td={2}", testName, T1, Td);
				}
			}
		}
			
		public virtual bool EnableTest(MethodInfo method)
		{
			return true;	
		}
	}
}


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
		public static TimeSpan CurrentTestDuration { get { return DateTime.Now - CurrentTestStartTime; } }
		
		public static readonly List<MethodInfo> Tests = new List<MethodInfo>();
		public static readonly List<DateTime> TestStartTimes = new List<DateTime>();
		public static readonly List<DateTime> TestFinishTimes = new List<DateTime>();
		public static readonly List<TimeSpan> TestDurations = new List<TimeSpan>();
		public static TimeSpan TotalTestsDuration { get { return TimeSpan.FromTicks(TestDurations.Sum(duration => duration.Ticks)); } }
		public static int TestFailures { get; protected set; }
		public static readonly List<Exception> TestExceptions = new List<Exception>();
		
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

		public virtual bool IncludeTest(MethodInfo testMethod) { return true; }
		public virtual bool OnStartingTests() { return true; }
		public virtual void OnStartedTests() { }
		public virtual bool OnStartingTest(MethodInfo testMethod) { return true; }
		public virtual void OnStartedTest(MethodInfo testMethod, DateTime startTime) { }
		public virtual void OnFailedTest(MethodInfo testMethod, DateTime startTime, Exception Exception) { }
		public virtual void OnFinishedTest(MethodInfo testMethod, bool success, DateTime startTime, DateTime finishTime, TimeSpan duration) { }
		public virtual void OnFinishedTests(
			IEnumerable<MethodInfo> tests,
			IEnumerable<DateTime> startTimes,
			IEnumerable<DateTime> finishTimes,
			IEnumerable<TimeSpan> durations,
			TimeSpan totalDuration,
			int totalFailed,
			IEnumerable<Exception> exceptions)
		{ }
		
		public void Run()
		{
			Type thisType = this.GetType();
			IEnumerable<MethodInfo> testMethods = thisType.GetMethods()
				.Where(mi => mi.GetCustomAttribute<TestAttribute>() != null && IncludeTest(mi));
			if (OnStartingTests())
			{
				OnStartedTests();
				foreach (MethodInfo mi in testMethods)
				{
					if (!OnStartingTest(mi))
						continue;
					CurrentTest = mi;
					DateTime T1 = CurrentTestStartTime = DateTime.Now;
					string testName = string.Concat(mi.DeclaringType.FullName, ".", mi.Name);
					bool testSuccess = false;
					try
					{
						Log.InfoFormat("\n--------Test: {0}-------- {1}", testName, T1);
						_writer.WriteLine("\n--------Test: {0}-------- {1}", testName, T1);
						OnStartedTest(mi, CurrentTestStartTime);
						mi.Invoke(this, new object[] { });
						testSuccess = true;
					}
//					catch (WebServiceException e) { clientWriter.WriteLine("ERROR: " + ex.Format()); }
//					catch (TargetInvocationException e) { clientWriter.WriteLine("ERROR: " + ex.Format()); }
					catch (Exception ex) {
						testSuccess = false;
						TestFailures++;
						TestExceptions.Add(ex);
						OnFailedTest(mi, T1, ex);
						DateTime Te = DateTime.Now;
						Log.ErrorFormat("\n--!-- !- ERROR: {0}    {1}\n{2}", ex.ToString(), Te, ex.Format());
						_writer.WriteLine(string.Format("\n--!-- !- ERROR: {0}    {1}\n{2}", ex.ToString(), Te, ex.Format()));
					}
					finally
					{
						DateTime T2 = DateTime.Now;
						TimeSpan Td = T2 - T1;
						TestStartTimes.Add(T1);
						TestFinishTimes.Add(T2);
						TestDurations.Add(Td);
						Tests.Add(mi);
						CurrentTest = null;
						CurrentTestStartTime = default(DateTime);
						OnFinishedTest(mi, testSuccess, T1, T2, Td);
						Log.InfoFormat("\\n--------Finished: {0}-------- {1} Td={2}", testName, T2, Td);
						_writer.WriteLine("\n--------Finished: {0}-------- {1} Td={2}", testName, T1, Td);
					}
				}
			}
			OnFinishedTests(Tests, TestStartTimes, TestFinishTimes, TestDurations, TotalTestsDuration, TestFailures, TestExceptions);
		}
	}
}


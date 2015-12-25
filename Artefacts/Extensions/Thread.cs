using System;
using System.Threading;

namespace Artefacts.Extensions
{
	public static class Thread_Ext
	{
		public static bool IsRunning(this Thread thread)
		{
			return thread.ThreadState.HasFlag(ThreadState.Running);
//				thread.ThreadState == ThreadState.Unstarted ||
//				thread.ThreadState == ThreadState.Running ||
//				thread.ThreadState == ThreadState.Stopped;
		}
	}
}


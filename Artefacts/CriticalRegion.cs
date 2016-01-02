using System;
using System.Threading;

namespace Artefacts
{
	public class CriticalRegion : IDisposable
	{
//		private 
		public CriticalRegion()
		{
			Thread.BeginCriticalRegion();
		}
		
		public virtual void Dispose()
		{
			Thread.EndCriticalRegion();
		}
	}
}


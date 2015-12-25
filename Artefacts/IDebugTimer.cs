using System;

namespace Artefacts
{
	public interface IDebugTimerTarget
	{
		DateTime _timeCreated { get; set; }
		DateTime _timeDestroyed { get; set; }
		TimeSpan _totalTime { get; set; }

		
	}
}


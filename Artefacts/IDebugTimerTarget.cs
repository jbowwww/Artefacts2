using System;

namespace Artefacts
{
	public interface IDebugTimerTarget
	{
		DateTime TimeCreated { get; set; }
		DateTime TimeDestroyed { get; set; }
		TimeSpan TotalTime { get; set; }
		TimeSpan TotalTimeUsed { get; set; }
	}
}


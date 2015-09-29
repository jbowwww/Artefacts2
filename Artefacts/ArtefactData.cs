using System;
using System.Collections.Generic;

namespace Artefacts
{
	public class ArtefactData : Dictionary<string, object>
	{
		public ArtefactData()
		{
		}

		public ArtefactData(Dictionary<string, object> values)
		{
			AddValues(values);
		}

		public void AddValues(IEnumerable<KeyValuePair<string, object>> values)
		{
			foreach (KeyValuePair<string, object> pair in values)
				base.Add(pair.Key, pair.Value);
		}
	}
}

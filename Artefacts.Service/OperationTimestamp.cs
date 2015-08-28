using System;

namespace Artefacts.Service
{
	public class OperationTimestamp
	{
		public DateTime Created {
			get;
			set;
		}
		
		public DateTime? Serialized {
			get;
			set;
		}
		
		public bool IsSerialized {
			get
			{
				return Serialized.HasValue;
			}
		}
		
		public OperationTimestamp()
		{
			Created = DateTime.Now;
			Serialized = null;
		}
		
		public DateTime MarkAsSerialized()
		{
			Serialized = (DateTime?)DateTime.Now;
			return Serialized.Value;
		}
	}
}


using System;
using System.Runtime.Serialization;

namespace Artefacts.Service
{
	[DataContract]
	public class CountResults
	{
		[DataMember]
		public int Count { get; set; }
		public CountResults()
		{
		}
		
		public override string ToString()
		{
			return string.Format("[CountResults: Count={0}]", Count);
		}
	}
}


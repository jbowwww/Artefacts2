using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Linq;

namespace Artefacts
{
//	[Serializable]
	[DebuggerDisplay("DataDictionary(Count={Count})")]
//	[DebuggerTypeProxy(typeof(DataDictionaryDebugView))]
	public class DataDictionary : Dictionary<string, object>//, ISerializable
	{
		public DataDictionary()
		{
		}

		public DataDictionary(Dictionary<string, object> values)
		{
			AddValues(values);
		}

		public void AddValues(IEnumerable<KeyValuePair<string, object>> values)
		{
			foreach (KeyValuePair<string, object> pair in values)
				base.Add(pair.Key, pair.Value);
		}
		
//		public override void GetObjectData(SerializationInfo info, StreamingContext context)
//		{
//			info.SetType(typeof(DataDictionary));
//			foreach (KeyValuePair<string, object> pair in this)
//				info.AddValue(pair.Key, pair.Value, pair.Value == null ? typeof(object) : pair.Value.GetType());
////			base.GetObjectData(info, context);
//		}
		
		internal class DataDictionaryDebugView
		{
			[DebuggerDisplay("{Key}: {Value}")]
			internal class KVP
			{
				string Key;
				object Value;
				public KVP(string key, object value)
				{
					this.Key = key;
					this.Value = value;
				}
			}
			
			IDictionary<string, object> _dictionary;
			
			DataDictionaryDebugView(DataDictionary dataDictionary)
			{
				_dictionary = dataDictionary as IDictionary<string, object>;
			}
			
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			KVP[] KeyValuePairs
			{
				get
				{
					return
						_dictionary == null ? new KVP[0] :
						(_dictionary as ICollection<KeyValuePair<string, object>>)
							.Select(kvp => new KVP(kvp.Key, kvp.Value)).ToArray();
				}
			}
			
		}
	}
}

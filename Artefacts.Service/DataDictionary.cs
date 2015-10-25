using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Linq;
using System.ComponentModel;
using System.Collections.Specialized;

namespace Artefacts
{
//	[Serializable]
	[DebuggerDisplay("DataDictionary(Count={Count})")]
	[DebuggerTypeProxy(typeof(DataDictionaryDebugView))]
	public class DataDictionary : Dictionary<string, object>
	{
		/// <summary>
		/// Timestamp when last this <see cref="Artefact"/> was saved/sent to server
		/// </summary>
		/// <value>The time saved.</value>
		public DateTime TimeChanged {
			get;
			private set;
		}

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
		
		/// <summary>
		/// Gets or sets the <see cref="Artefacts.DataDictionary"/> with the specified key.
		/// Overrides base implementation for purpose of tracking when data changes
		/// </summary>
		/// <param name="key">Key.</param>
		/// <remarks>
		/// WIth data change timestamps, may need to handle differently for value/reference types
		/// </remarks>
		public new object this[string key]
		{
			get
			{
				return base[key];
			}
			set
			{
				if (base.ContainsKey(key) && base[key] != value)
					TimeChanged = DateTime.Now;
				base[key] = value;
			}
		}
		
//		public override void GetObjectData(SerializationInfo info, StreamingContext context)
//		{
//			info.SetType(typeof(DataDictionary));
//			foreach (KeyValuePair<string, object> pair in this)
//				info.AddValue(pair.Key, pair.Value, pair.Value == null ? typeof(object) : pair.Value.GetType());
////			base.GetObjectData(info, context);
		//		}
		
//		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
//		DataDictionaryDebugView.KVP[] DataItems {
//			get
//			{
//				uint dataItemsCount = 0;
//				DataDictionaryDebugView.KVP[] dataItems = new DataDictionaryDebugView.KVP[base.Count];
//				foreach (KeyValuePair<string, object> data in this.AsEnumerable().ToArray<KeyValuePair<string, object>>())
//					dataItems[dataItemsCount++] = new DataDictionaryDebugView.KVP(data.Key, data.Value);
//				return dataItems;
//				return 
//			}
//		}

		[DebuggerDisplay("Count={Count()}")]
		internal class DataDictionaryDebugView
//			: ICollection<DataDictionaryDebugView.KVP>
		{
			[DebuggerDisplay("{Key}: {Value}")]
			public class KVP
			{
				string Key;
				object Value;
				public KVP(string key, object value)
				{
					this.Key = key;
					this.Value = value;
				}
			}
			
//			private IDictionary<string, object> _dictionary;
			
			DataDictionaryDebugView(DataDictionary dataDictionary)
			{
//				_dictionary = dataDictionary;
				KeyValuePairs = dataDictionary.Select(kvp => new KVP(kvp.Key, kvp.Value)).ToArray();
					//new KVP[dataDictionary.Count];
				
			}
			
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			DataDictionaryDebugView.KVP[] KeyValuePairs {
				get;
				 set;
//				{
//					return 
//						_dictionary == null ? new DataDictionaryDebugView.KVP[0] :
//							(_dictionary as ICollection<KeyValuePair<string, object>>)
//							.Select(kvp => new DataDictionaryDebugView.KVP(kvp.Key, kvp.Value)).ToArray();
//				}
			}

		}
	}
}

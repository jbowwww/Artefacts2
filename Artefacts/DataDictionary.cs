using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Linq;

namespace Artefacts
{
//	[Serializable]
	[DebuggerDisplay("DataDictionary(Count={Count})")]
	[DebuggerTypeProxy(typeof(DataDictionaryDebugView))]
	[CollectionDataContract]
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

		/// <summary>
		/// Gets or sets the <see cref="Artefacts.DataDictionary"/> with the specified key.
		/// Overrides base implementation for purpose of tracking when data changes
		/// </summary>
		/// <param name="key">Key.</param>
		/// <remarks>
		/// WIth data change timestamps, may need to handle differently for value/reference types
		/// </remarks>
		public new object this[string name]
		{
			get
			{
				object result;
				if (!(base.TryGetValue(name, out result)))
					throw new ArgumentOutOfRangeException("name", name, "Data member \"" + name + "\" not found in Artefact.Data");
				return result;
			}
			set
			{
				if (base.ContainsKey(name) && base[name] != value)
					TimeChanged = DateTime.Now;
				if (!base.ContainsKey(name))
					base.Add(name, value);
				else
					base[name] = value;
			}
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
		
//		public override void GetObjectData(SerializationInfo info, StreamingContext context)
//		{
//			info.SetType(typeof(DataDictionary));
//			foreach (KeyValuePair<string, object> pair in this)
//				info.AddValue(pair.Key, pair.Value, pair.Value == null ? typeof(object) : pair.Value.GetType());
//		//	base.GetObjectData(info, context);
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

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public DataDictionaryDebugView.KVP[] KeyValuePairs {
				get;
				set;
			}
			
			DataDictionaryDebugView(DataDictionary dataDictionary)
			{
				KeyValuePairs = dataDictionary.Select(kvp => new KVP(kvp.Key, kvp.Value)).ToArray();
			}
		}
	}
}

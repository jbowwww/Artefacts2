using System;
using Artefacts;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.Serialization;
using ServiceStack;

namespace Artefacts.Service
{
//	[Route("/{CollectionName}/Query/", "GET")]
	[CollectionDataContract]
	public class QueryResults : ICollection<Artefact>
	{
//		[DataMember(Order=1)]
		public List<Artefact> /*Artefact[]*/ Artefacts {
			get;
			set;
		}
		
//		[DataMember(Order = 2)]
//		public ResponseStatus ResponseStatus { get; set; }
		
		public QueryResults()
		{
			//Artefacts = new Artefact[0];
			Artefacts = new List<Artefact>();
		}
		
		public QueryResults(IEnumerable<Artefact> artefacts)
		{
			//Artefacts = new List<Artefact>(artefacts).ToArray();
			Artefacts = new List<Artefact>(artefacts);
		}
		
		public IEnumerable<T> Get<T>() where T : new()
		{
			return Artefacts.Select(artefact => artefact.As<T>());
		}

		#region ICollection implementation

		public void Add(Artefact item)
		{
//			Artefact[] artefacts = Artefacts;
//			Artefacts = new Artefact[artefacts.Length + 1];
//			artefacts.CopyTo(Artefacts, 0);
//			Artefacts[Artefacts.Length - 1] = item;
			Artefacts.Add(item);
		}

		public void Clear()
		{
			Artefacts.Clear();
		}

		public bool Contains(Artefact item)
		{
			return Artefacts.Contains(item);
		}

		public void CopyTo(Artefact[] array, int arrayIndex)
		{
			Artefacts.CopyTo(array, arrayIndex);
		}

		public bool Remove(Artefact item)
		{
			return Artefacts.Remove(item);
		}

		public int Count {
			get {
				return Artefacts.Count();
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		#endregion

		#region IEnumerable implementation
		public IEnumerator<Artefact> GetEnumerator()
		{
			return Artefacts/*.AsEnumerable()*/.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)this.GetEnumerator();
		}
		#endregion
		
		public override string ToString()
		{
			return string.Format("[QueryResults: Artefacts.Count={0}]",	//, Artefacts={1}]",	//, ResponseStatus={2}]",
				Artefacts == null ? "[null]" : Artefacts.Count().ToString(),
				Artefacts == null ? "[null]" : Artefacts.ToString());
			//ResponseStatus == null ? "[null]" : ResponseStatus.ToString());
		}
	}
}


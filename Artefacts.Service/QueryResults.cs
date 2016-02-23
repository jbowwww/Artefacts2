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
//	[CollectionDataContract]
	[DataContract]
	public class QueryResults : ICollection<Artefact>
	{
		#region Properties
		/// <summary>Gets or sets the artefacts</summary>
		[DataMember]
		public List<Artefact> Artefacts { get; set; }
		
		/// <summary>Gets the count</summary>
		/// <remarks>ICollection implementation</remarks>
//		[DataMember(Order=1)]
		public int Count { get { return Artefacts == null ? 0 : Artefacts.Count; } } 
		
		/// <summary>Gets or sets the server count</summary>
//		[DataMember(Order=3)]
//		public int ServerCount { get; set; }
		
		/// <summary>Gets a value indicating whether this instance is read only</summary>
		/// <remarks>ICollection implementation</remarks>
		public bool IsReadOnly { get { return false; } }
		#endregion
		
		#region Construction
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Service.QueryResults"/> class.
		/// </summary>
		public QueryResults()
		{
			Artefacts = new List<Artefact>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Service.QueryResults"/> class.
		/// </summary>
		/// <param name="count">Count.</param>
		public QueryResults(int count)
		{
			Artefacts = new List<Artefact>(count);
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Service.QueryResults"/> class.
		/// </summary>
		/// <param name="artefacts">Artefacts.</param>
		public QueryResults(IEnumerable<Artefact> artefacts)
		{
			Artefacts = new List<Artefact>(artefacts);
		}
		#endregion
		
		/// <summary>
		/// Get this instance.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public IEnumerable<T> Get<T>() where T : new()
		{
			return Artefacts.Select(artefact => artefact.As<T>());
		}

		#region ICollection implementation
		public void Add(Artefact item)
		{
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
			return string.Format("[QueryResults: Artefacts.Count={0} Count={1}]",
				Artefacts == null ? "[null]" : Artefacts.Count().ToString(),
				Artefacts == null ? "[null]" : Artefacts.ToString());
			//ResponseStatus == null ? "[null]" : ResponseStatus.ToString());
		}
	}
}


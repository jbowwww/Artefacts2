using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using ServiceStack;
using Serialize.Linq.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver;

namespace Artefacts.Service
{
	public class ArtefactQueryable<T> : IQueryable<T>, IQueryable, ICollection<T>
	{
		#region Private fields
//		private Expression _expression;
		private QueryResults _results;
		#endregion
		
		#region Constants
		public readonly Type ElementType = typeof(T);
		public readonly Type EnumerableType = typeof(IEnumerable<T>);
		public readonly Type QueryableType = typeof(IQueryable<T>);
		public readonly Type EnumerableStaticType = typeof(System.Linq.Enumerable);
		public readonly Type QueryableStaticType = typeof(System.Linq.Queryable);
		public readonly Type QueryResultType = typeof(QueryResults);
		#endregion
		
		#region Properties and fields
		Type IQueryable.ElementType {
			get { return ElementType; }
		}
		public Expression Expression { get; protected set; }
		public IQueryProvider Provider {
			get { return (IQueryProvider)Collection; }
		}
		public IArtefactCollection Collection { get; protected set; }
		public IMongoQuery MongoQuery { get; protected set; }
		public QueryResults Results {
			get { return _results ?? (_results = (QueryResults)Provider.Execute(Expression)); }
		}

		public int Count {
			get { return Results.ServerCount; }
		}

		public bool IsReadOnly {
			get { return false; }
		}
		#endregion
		
		#region Construction
		public ArtefactQueryable() { }
		public ArtefactQueryable(IArtefactCollection collection, Expression expression)
		{
			Expression = expression;
			Collection = collection;
		}
		#endregion
		
		#region ICollection implementation

		public void Add(T item)
		{
			throw new NotImplementedException();
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(T item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(T item)
		{
			throw new NotImplementedException();
		}
		#endregion
		#region IEnumerable implementations
		public IEnumerator<T> GetEnumerator()
		{
//			if (ElementType == typeof(Artefact))	//artefactType)
//				return /*(IEnumerator<T>)*/Results.Artefacts.GetEnumerator();
			return Results.Artefacts.Select(a => a.As<T>()).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)GetEnumerator();
		}
		#endregion
	}
	
	public static class ArtefactQueryableExtensions
	{
		public static int Count<T>(this ArtefactQueryable<T> artefactQueryable)
		{
			return artefactQueryable.Results.Count;
		}
	}
}


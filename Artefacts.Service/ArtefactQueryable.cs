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
		public ServiceStack.Logging.ILog _log = null;
		public ServiceStack.Logging.ILog Log {
			get
			{
				return _log ??
					(_log = Artefact.LogFactory.GetLogger(this.GetType()));
			}
		}

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
		protected ArtefactQueryable() { }
		public ArtefactQueryable(IArtefactCollection collection, Expression expression)
		{
			Expression = expression;
			Collection = collection;
			Log.Info(this);
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
			Log.DebugFormat("ArtefactQueryable<{0}>.GetEnumerator()", typeof(T).FullName);
			return Results.Artefacts.Select(a => a.As<T>()).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)GetEnumerator();
		}
		#endregion
		
		public override string ToString()
		{
			return string.Format("[ArtefactQueryable<{0}>: {1}]", typeof(T).FullName, Expression);
				//string.Format("[ArtefactQueryable: Log={0}, Expression={1}, Provider={2}, Collection={3}, MongoQuery={4}, Results={5}, Count={6}, IsReadOnly={7}]", Log, Expression, Provider, Collection, MongoQuery, Results, Count, IsReadOnly);
		}
	}
	
	public static class ArtefactQueryableExtensions
	{
		public static int Count<T>(this ArtefactQueryable<T> artefactQueryable)
		{
			return artefactQueryable.Results.Count;
		}
	}
}


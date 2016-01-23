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
	public class ArtefactQueryable<T> : IQueryable<T>
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
		public Expression Expression {
			get;// { return _expression; }
			protected set;
//			{
//				_expression = value;
//				Translate(_expression);
//			}
		}
		public IQueryProvider Provider {
			get { return (IQueryProvider)Collection; }
		}
		public ArtefactCollection<T> Collection {
			get;	
			protected set;
		}
		public IMongoQuery MongoQuery {
			get;
			protected set;
		}
		public QueryResults Results {
			get { return _results ?? (_results = (QueryResults)Provider.Execute(Expression)); }
		}
		#endregion
		
		#region Construction
		public ArtefactQueryable() { }
		public ArtefactQueryable(ArtefactCollection<T> collection, Expression expression)
		{
			Expression = expression;
			Collection = collection;
		}
		#endregion
		
		#region IEnumerable implementations
		public IEnumerator<T> GetEnumerator()
		{
			if (ElementType == typeof(Artefact))	//artefactType)
				return (IEnumerator<T>)Results.Artefacts;
			return Results.Artefacts.Select(a => a.As<T>()).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)GetEnumerator();
		}
		#endregion
	}
}


using System;
using System.Linq;

namespace Artefacts.Service
{
	public class ArtefactsCollection<T> : IQueryProvider, IQueryable<Artefact>, IQueryable<T>
	{
		public ArtefactsCollection()
		{
		}

		#region IQueryProvider implementation

		public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
		{
			
		}

		public object Execute(System.Linq.Expressions.Expression expression)
		{
			throw new NotImplementedException();
		}

		public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
		{
			throw new NotImplementedException();
		}

		public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable implementation

		public System.Collections.Generic.IEnumerator<Artefact> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IQueryable implementation

		public Type ElementType {
			get {
				throw new NotImplementedException();
			}
		}

		public System.Linq.Expressions.Expression Expression {
			get {
				throw new NotImplementedException();
			}
		}

		public IQueryProvider Provider {
			get {
				throw new NotImplementedException();
			}
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}


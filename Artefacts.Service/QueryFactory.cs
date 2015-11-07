

namespace Artefacts.Service
{
	/// <summary>
	/// Query factory.
	/// </summary>
	public class QueryFactory
	{
		/// <summary>
		/// Gets the visitor.
		/// </summary>
		public virtual ExpressionVisitor Visitor {
			get { return ClientQueryVisitor.Singleton; }
		}

		/// <summary>
		/// Create the specified predicate.
		/// </summary>
		/// <param name="predicate">Predicate.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public QueryRequest Create<T>(Expression<Func<T, bool>> predicate)
		{
			return new QueryRequest(Visitor.Visit(predicate));
		}
	}
}


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Artefacts;
using ServiceStack;
using MongoDB.Bson;
using System.Collections.ObjectModel;

namespace Artefacts.Service
{
	public class ArtefactCollection : ArtefactCollection<Artefact>
	{
		public ArtefactCollection(IServiceClient serviceClient, string collectionName)
			: base(serviceClient, collectionName) { }
	}
	
	public class ArtefactCollection<T> : IQueryProvider, IQueryable<T>
	{
		#region Private fields
		static readonly Type _enumerableStaticType = typeof(System.Linq.Enumerable);
		static readonly Type _queryableStaticType = typeof(System.Linq.Queryable);
		static readonly Type _queryResultType = typeof(QueryResults);
		static readonly Type _artefactType = typeof(Artefact);
		readonly Type _elementType;
		readonly Type _enumerableType;
		readonly Type _queryableType;
		
		private ClientQueryVisitor<T> _visitor = new ClientQueryVisitor<T>();
		private QueryResults _results;
		#endregion
		
		#region Properties
		/// <summary>
		/// Gets or sets the client.
		/// </summary>
		public IServiceClient Client {
			get;
			protected set;
		}
		
		/// <summary>
		/// Gets or sets the name of the collection.
		/// </summary>
		public string CollectionName {
			get;
			protected set;
		}
		
		/// <summary>
		/// Gets or sets the results.
		/// </summary>
		public QueryResults Results {
			get { return _results ?? (_results = (QueryResults)Execute(Expression)); }
		}
		
		/// <summary>
		/// Gets or sets the type of the element.
		/// </summary>
		/// <remarks>IQueryable implementation</remarks>
		public Type ElementType {
			get { return _elementType; }
		}
		
		/// <summary>
		/// Gets or sets the expression.
		/// </summary>
		/// <remarks>IQueryable implementation</remarks>
		public Expression Expression {
			get;
			protected set;
		}
		
		/// <summary>
		/// Gets or sets the provider.
		/// </summary>
		/// <remarks>IQueryable implementation</remarks>
		public IQueryProvider Provider {
			get { return this; }
		}
		#endregion
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Service.ArtefactsCollection`1"/> class.
		/// </summary>
		/// <param name="client">Client.</param>
		/// <param name="collectionName">Collection name.</param>
		/// <param name="expression">Expression.</param>
		public ArtefactCollection(IServiceClient client, string collectionName, Expression expression = null)
		{
			if (client == null)
				throw new ArgumentNullException("serviceClient");
			if (collectionName.IsNullOrSpace())
				throw new ArgumentNullException("collectionName");
			
			_elementType = typeof(T);
			_enumerableType = typeof(IEnumerable<T>);
			_queryableType = typeof(IQueryable<T>);
			
			Client = client;
			CollectionName = collectionName;
			Expression = expression ?? Expression.Parameter(typeof(IQueryable<T>), "collection");
		}

		#region IQueryProvider implementation
		/// <Docs>To be added.</Docs>
		/// <returns>To be added.</returns>
		/// <summary>
		/// Creates the query.
		/// </summary>
		/// <param name="expression">Expression.</param>
		public IQueryable CreateQuery(Expression expression)
		{
			return (IQueryable)CreateQuery<T>(expression);
		}

		/// <Docs>To be added.</Docs>
		/// <returns>To be added.</returns>
		/// <summary>
		/// Creates the query.
		/// </summary>
		/// <param name="expression">Expression.</param>
		/// <typeparam name="TElement">The 1st type parameter.</typeparam>
		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			return new ArtefactCollection<TElement>(Client, CollectionName, expression);
		}

		/// <Docs>To be added.</Docs>
		/// <returns>To be added.</returns>
		/// <summary>
		/// Execute the specified expression.
		/// </summary>
		/// <param name="expression">Expression.</param>
		public object Execute(Expression expression)
		{
			return Client.Get<QueryResults>(new QueryRequest(CollectionName, _visitor.Visit(expression)));
		}

		/// <Docs>To be added.</Docs>
		/// <returns>To be added.</returns>
		/// <summary>
		/// Execute the specified expression.
		/// </summary>
		/// <param name="expression">Expression.</param>
		/// <typeparam name="TResult">The 1st type parameter.</typeparam>
		public TResult Execute<TResult>(Expression expression)
		{
			Type _resultType = typeof(TResult);
			
			expression = _visitor.Visit(expression);
			
			MethodCallExpression mce = expression as MethodCallExpression;
			if (mce != null)
			{
				if (!_resultType.IsAssignableFrom(mce.Method.ReturnType))
					throw new ArgumentOutOfRangeException("TResult", _resultType, "TResult type \"" + _resultType.FullName + "\" for expression \"" + expression + "\" should have been assignable from \"" + mce.Method.ReturnType.FullName + "\"");
				if (mce.Method.DeclaringType == _enumerableStaticType || mce.Method.DeclaringType == _queryableStaticType)
				{
					if (!typeof(QueryResults).IsAssignableFrom(_resultType) &&
					    !_enumerableType.IsAssignableFrom(_resultType) &&
					    !_queryableType.IsAssignableFrom(_resultType))
					{
						if (mce.Arguments[0] == null)
							throw new NullReferenceException("Method Call \"" + mce + "\" inner object is null");
						if (mce.Arguments[0].Type != _enumerableType && mce.Arguments[0].Type != _queryableType)
							throw new ArgumentOutOfRangeException("expression", expression, "Expression too complex: Method Call \"" + mce + "\" inner object type is not \"" + _enumerableType.FullName + "\" or \"" + _queryableType.FullName + "\"");
						QueryResults qr = (QueryResults)Execute(mce.Arguments[0]);
						if (mce.Method.Name == "Count" && mce.Arguments.Count == 1)
							return (TResult)(object)qr.Count;
						return (TResult)mce.Method.Invoke(null,
							new[] { qr.Select(a => a.As(_resultType.GetElementType())) }.Concat(
								mce.Arguments.Skip(1).Select(e => (e as ConstantExpression).Value)
							).ToArray());
					}
				}
				return (TResult)Execute(mce);
			}
			
			if (_resultType != _queryResultType)
				throw new ArgumentOutOfRangeException("TResult", _resultType, "TResult type \"" + _resultType.FullName + "\" for expression \"" + expression + "\" should have been \"" + _queryResultType.FullName + "\"");
			
			return (TResult)Execute(expression);
		}
		#endregion
		
		#region IEnumerable implementations
		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			if (_elementType == _artefactType)
				return (IEnumerator<T>)Results.Artefacts;
			return Results.Artefacts.Select(a => a.As<T>()).GetEnumerator();
		}

		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// <returns>The enumerator.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return (IEnumerator)GetEnumerator();
		}

		#endregion
	}
}


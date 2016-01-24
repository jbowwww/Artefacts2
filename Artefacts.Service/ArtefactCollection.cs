using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Artefacts;
using ServiceStack;
using MongoDB.Bson;
using System.Collections.ObjectModel;
using MongoDB.Driver.Linq;
using Serialize.Linq.Serializers;
using MongoDB.Driver;

namespace Artefacts.Service
{	
	public class ArtefactCollection
	{
		public readonly static Dictionary<Type, IArtefactCollection> CollectionsByType = new Dictionary<Type, IArtefactCollection>();

	}
	
	public class ArtefactCollection<T> : ArtefactQueryable<T>, IArtefactCollection, IQueryProvider, IQueryable<T>, IDisposable
	{
		
		#region Private fields
		protected readonly ClientQueryVisitor<T> _visitor = new ClientQueryVisitor<T>();
		protected readonly ArtefactQueryTranslator<T> _translator = new ArtefactQueryTranslator<T>();
		#endregion
		
		#region Properties
		public IServiceClient Client {
			get;
			protected set;
		}
		public string Name {
			get;
			protected set;
		}
		#endregion

		#region Construction & disposal
//		public ArtefactCollection() : base() { }
		public ArtefactCollection(IServiceClient client, string name, Expression expression = null)
		{
			if (client == null)
				throw new ArgumentNullException("serviceClient");
			if (name.IsNullOrSpace())
				throw new ArgumentNullException("collectionName");
			
			if (ArtefactCollection.CollectionsByType.ContainsKey(typeof(T)))
				throw new InvalidOperationException(string.Format("An instance of type ArtefactCollection<{0}> already exists", typeof(T).FullName));
			ArtefactCollection.CollectionsByType.Add(typeof(T), this);
			
			Client = client;
			Name = name;
			Collection = this;
			Expression = expression ?? Expression.Constant(this);		//Expression.Parameter(typeof(ArtefactCollection<T>), "collection"))
		}
		public void Dispose()
		{
			if (!ArtefactCollection.CollectionsByType.ContainsKey(ElementType))
				throw new InvalidOperationException("ArtefactCollection.CollectionsByType does not have an entry with key type \"" + ElementType.FullName + "\"");
			ArtefactCollection.CollectionsByType.Remove(ElementType);
		}
		#endregion

		#region IQueryProvider implementation
		public IQueryable CreateQuery(Expression expression)
		{
			return new ArtefactQueryable<T>(this, expression);
		}
		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			return new ArtefactQueryable<TElement>(
				(ArtefactCollection<TElement>)ArtefactCollection.CollectionsByType[typeof(TElement)],
				expression);
		}
		public object Execute(Expression expression)
		{
			Expression visitedExpression = _visitor.Visit(expression);
			IMongoQuery translatedQuery = _translator.Translate(visitedExpression);
//			QueryDocument queryDoc;
			return Client.Get<QueryResults>(new QueryRequest(Name, translatedQuery));
		}
		/// <Docs>To be added.</Docs>
		/// <returns>To be added.</returns>
		/// <summary>
		/// Execute the specified expression.
		/// </summary>
		/// <param name="expression">Expression.</param>
		/// <typeparam name="TResult">The 1st type parameter.</typeparam>
		/// <remarks>
		/// Going to have to think carefully now about how things need to be done. How to handle other LINQ extensions methods
		/// You will prob need to extract from the expressions the queryable "entities" that are needed for either results,
		/// or functions that return scalars (and they will need to be handled specially if you want things handled server side
		/// wherever possible).
		/// Might need to write some example test queries to use as unit tests. It will take a while to get all (or most) of
		/// the LINQ methods working
		/// </remarks>
		public TResult Execute<TResult>(Expression expression)
		{
			Type resultType = typeof(TResult);
			
			expression = _visitor.Visit(expression);
			
			MethodCallExpression mce = expression as MethodCallExpression;
			if (mce != null)
			{
				if (!resultType.IsAssignableFrom(mce.Method.ReturnType))
					throw new ArgumentOutOfRangeException("TResult", resultType, "TResult type \"" + resultType.FullName + "\" for expression \"" + expression + "\" should have been assignable from \"" + mce.Method.ReturnType.FullName + "\"");
				if (mce.Method.DeclaringType == EnumerableStaticType || mce.Method.DeclaringType == QueryableStaticType)
				{
					if (!typeof(QueryResults).IsAssignableFrom(resultType) &&
					    !EnumerableType.IsAssignableFrom(resultType) &&
					    !QueryableType.IsAssignableFrom(resultType))
					{
						if (mce.Arguments[0] == null)
							throw new NullReferenceException("Method Call \"" + mce + "\" inner object is null");
						if (!EnumerableType.IsAssignableFrom(mce.Arguments[0].Type))	// (mce.Arguments[0].Type != EnumerableType && mce.Arguments[0].Type != QueryableType)
							throw new ArgumentOutOfRangeException("expression", expression, "Expression too complex: Method Call \"" + mce + "\" inner object type does not implement \"" + EnumerableType.FullName + "\"");
						QueryResults qr = (QueryResults)Execute(mce.Arguments[0]);
						if (mce.Method.Name == "Count" && mce.Arguments.Count == 1)
							return (TResult)(object)qr.Count;
						return (TResult)mce.Method.Invoke(null,
						                                 // new[] { QueryableType },
						                                  new [] { qr });// qr.Select(a => a.As(resultType.GetElementType())) }.Concat(
//								mce.Arguments.Skip(1).Select(e => (e as ConstantExpression).Value)
//							).ToArray());
					}
				}
				return (TResult)Execute(mce);
			}
			
			if (resultType != QueryResultType)
				throw new ArgumentOutOfRangeException("TResult", resultType, "TResult type \"" + resultType.FullName + "\" for expression \"" + expression + "\" should have been \"" + QueryResultType.FullName + "\"");
			
			return (TResult)Execute(expression);
		}
		#endregion
	}
}


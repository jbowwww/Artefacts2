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
using ServiceStack.Logging;

namespace Artefacts.Service
{	
	public class ArtefactCollection
	{
		public readonly static Dictionary<Type, IArtefactCollection> CollectionsByType = new Dictionary<Type, IArtefactCollection>();
		public static ArtefactCollection<T> GetOrCreate<T>(IServiceClient client, string name = "", Expression expression = null)
		{
			return (ArtefactCollection<T>)(CollectionsByType.ContainsKey(typeof(T))
				? CollectionsByType[typeof(T)]
				: (CollectionsByType[typeof(T)] = new ArtefactCollection<T>(client, name, expression)));
		}
	}
	
	/// <summary>
	/// Artefact collection.
	/// </summary>
	/// <remarks>
	/// TODO: Changed change from by type to keyed by name? That way:
	/// Back end mongo doesn't care about type - why should this
	/// No dilemma about single/multiple collections (in logical sense) for derived types
	/// </remarks>
	public class ArtefactCollection<T> : ArtefactQueryable<T>, IArtefactCollection, IDisposable
	{
		#region Static members
		public static ExpressionVisitor Visitor = new ClientQueryVisitor<T>();
		#endregion
		
		#region Properties
		public IServiceClient Client { get; protected set; }
		public string Name { get; protected set; }
		#endregion

		#region Construction & disposal
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Service.ArtefactCollection`1"/> class.
		/// </summary>
		/// <param name="client">Client.</param>
		/// <param name="expression">Expression.</param>
		/// <param name="name">Name.</param>
		/// <remarks>
		/// TODO: Change so indexes collections by name instead of type
		/// </remarks>
		private void Init(IServiceClient client, Expression expression, string name)
		{
			if (client == null)
				throw new ArgumentNullException("serviceClient");	
			if (name.IsNullOrSpace())
			{
				if (ArtefactCollection.CollectionsByType.ContainsKey(ElementType))
					throw new InvalidOperationException(string.Format("An instance of type ArtefactCollection<{0}> already exists", ElementType.FullName));
				ArtefactCollection.CollectionsByType.Add(ElementType, this);
				name = Artefact.MakeSafeCollectionName(ElementType.FullName);
			}
			Client = client;
			Name = name;
			Collection = this;
			Expression = expression ?? Expression.Parameter(typeof(ArtefactCollection<T>), Name);
			Log.Info(this);
		}
		
		public ArtefactCollection(IServiceClient client, string name = "", Expression expression = null)
		{
			Init(client, expression, name);
		}
		
		public ArtefactCollection(IServiceClient client, string name, IEnumerable<T> initialItems)
		{
			Log.Debug(initialItems);
			if (name.IsNullOrSpace())
				throw new ArgumentOutOfRangeException("name", name, name == null ? "Name is null" : "Name is whitespace");
			Init(client, null, name);
			Insert(initialItems);
		}
		
		/// <summary>
		/// Releases all resource used by the <see cref="Artefacts.Service.ArtefactCollection`1"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Artefacts.Service.ArtefactCollection`1"/>.
		/// The <see cref="Dispose"/> method leaves the <see cref="Artefacts.Service.ArtefactCollection`1"/> in an unusable
		/// state. After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Artefacts.Service.ArtefactCollection`1"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Artefacts.Service.ArtefactCollection`1"/> was occupying.
		/// 
		/// TODO: Look up requirement for virtual (conditions requiring it, why, etc)
		/// </remarks>
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
			return CreateQuery<T>(expression);
		}
		
		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			IArtefactCollection collection = ArtefactCollection.GetOrCreate<TElement>(Client);
			Log.Debug(collection);
			expression = Visitor.Visit(expression);
			Log.Debug(expression);
			IQueryable<TElement> q = new ArtefactQueryable<TElement>(collection, expression);
			return q;
		}
		
		public object Execute(Expression expression)
		{
			IReturn request;
			Type resultType = expression.Type;
			expression = Visitor.Visit(expression);
			Log.Debug(expression);

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
						//QueryResults qr = (QueryResults)Execute(mce);//.Arguments[0]);

						if (mce.Method.Name == "Count" && mce.Arguments.Count == 1)
						{
							//return (TResult)(object)qr.ServerCount;
							//							return (TResult)(object)Client.Get<CountResults>(CountRequest.Make<T>(Name, mce)).Count;
							request = CountRequest.Make<T>(Name, mce);
							int cr = Client.Get<CountResults>(request).Count;
							Log.Info(new object[] { "GET", resultType, expression, request, cr });
							return cr;
						}

						throw new InvalidOperationException(string.Format("Unknown scalar LINQ method \"{0} {1}.{2}\"",
							mce.Method.ReturnType.FullName, mce.Method.DeclaringType.FullName, mce.Method.Name));
						//						return (TResult)mce.Method.Invoke(null, new [] { qr });
					}
//					else
//						throw new InvalidOperationException(string.Format("Not expected, Execute<{0}>(\"{1}\")", resultType.FullName, expression));
				}
				//				return (TResult)Execute(mce);
			}

//			if (resultType != QueryResultType)
//				throw new ArgumentOutOfRangeException("TResult", resultType, "TResult type \"" + resultType.FullName + "\" for expression \"" + expression + "\" should have been \"" + QueryResultType.FullName + "\"");

			request = QueryRequest.Make<T>(Name, expression);
			QueryResults qr = Client.Get<QueryResults>(request);
			Log.Info(new object[] { "GET", resultType, expression, request, qr });
//			Log.DebugFormat("ArtefactCollection.Execute<{0}>(\"{1}\"): Client.Get({2}): Result={3}", resultType.FullName, expression, request, qr);
			return qr;
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
			if (typeof(TResult) != expression.Type)
				throw new ArgumentOutOfRangeException("TResult", typeof(TResult), string.Format("TResult does not match expression return type of \"{0}\"", expression.Type.FullName));
			return (TResult)Execute(expression);
		}
		#endregion
		
		/// <summary>
		/// Insert the specified items.
		/// </summary>
		/// <param name="items">Items.</param>
		/// <remarks>
		/// TODO: Move this to an overrtiden implementation of ICollection.Add() ?
		/// </remarks>
		public int Insert(IEnumerable<T> items)
		{
			Log.Debug(items);
			SaveBatchRequest request = new SaveBatchRequest();
			request.Items = items.Select(item => new SaveRequest(this, SaveRequestType.Insert, Artefact.Cache.GetArtefact(item))).ToArray();
			Log.Debug(request);
			object response = Client.Post(request);
			Log.Debug(response);
			return request.Items.Length;
			
//			int c = 0;
//			foreach (T item in items)		// TODO : Test/implement batch/bulk insert
//			{
//				Log.Debug(new object[] { "PUT", item });
//				try
//				{
//					// TODO: Need a InsertArtefact SS request object
//					Client.Put(Artefact.Cache.GetArtefact(item));
//					c++;
//				}
//				catch (Exception ex)
//				{
//					Log.Error(item, ex);
//				}
//			}
//			return c;
		}
		
		/// <summary>
		/// Insert the specified item.
		/// </summary>
		/// <param name="item">Item.</param>
		/// <remarks>
		/// TODO: Move this to an overrtiden implementation of ICollection.Add() ?
		/// </remarks>
		public void Insert(T item)
		{
			//static object clientLock = new object();
			Log.Debug(item);
			SaveRequest request = new SaveRequest(this, SaveRequestType.Insert, Artefact.Cache.GetArtefact(item));
			Log.Debug(request);
			object response;
			lock (Client)
			{
				response = Client.Post(request);
			}
			Log.Debug(response);
		}
		
		public override string ToString()
		{
			return string.Concat("<<", Name, ">>",
				(Expression == null || Expression.NodeType != ExpressionType.Parameter ||
				((ParameterExpression)Expression).Name != typeof(ArtefactCollection<T>).FullName)
					? null : string.Concat(": ", ExpressionPrettyPrinter.PrettyPrint(Expression)));
		}
	}
}


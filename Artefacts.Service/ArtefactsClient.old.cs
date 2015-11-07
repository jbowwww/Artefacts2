using System;
using ServiceStack;
using System.Net;
using TextWriter = System.IO.TextWriter;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using ServiceStack.Text;
using ServiceStack.Logging;
using Artefacts.FileSystem;

namespace Artefacts.Service
{
	public class ArtefactsClient
	{
		#region Static members
		public static readonly ILogFactory LogFactory;
		public static readonly ILog Log;
		
		static ArtefactsClient()
		{
			LogFactory = new StringBuilderLogFactory();
			Log = ArtefactsClient.LogFactory.GetLogger(typeof(ArtefactsClient));
		}
		#endregion
		
		#region Private fields
		private TextWriter _bufferWriter;

		private string _serviceBaseUrl;

		private IServiceClient _serviceClient;

		private readonly Dictionary<object, Artefact> _artefacts = new Dictionary<object, Artefact>();
		#endregion
		
		#region Properties
		/// <summary>
		/// Gets or sets the visitor.
		/// </summary>
		public ExpressionVisitor Visitor { get; set; }
		#endregion
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Service.ArtefactsClient"/> class.
		/// </summary>
		/// <param name="serviceBaseUrl">Service base URL.</param>
		/// <param name="bufferWriter">Buffer writer.</param>
		public ArtefactsClient(string serviceBaseUrl, TextWriter bufferWriter)
		{
			_bufferWriter = bufferWriter;
			_serviceBaseUrl = serviceBaseUrl;
			this.Visitor = new ClientQueryVisitor();
			_bufferWriter.Write(string.Format("Creating client to access {0} ... ", _serviceBaseUrl));
			_serviceClient = new ServiceStack.JsvServiceClient(_serviceBaseUrl) {
				RequestFilter = (HttpWebRequest request) => bufferWriter.WriteLine(
					string.Format("Client.{0} HTTP {6} {2} bytes {1} Expect {7} Accept {8} {5}",//\n{9}\n",
			              request.Method, request.ContentType,  request.ContentLength,
			              request.UserAgent, request.MediaType, request.RequestUri,
			              request.ProtocolVersion, request.Expect, request.Accept,
						string.Format("\tHeaders: {0}", request.Headers))),
				ResponseFilter = (HttpWebResponse response) => bufferWriter.WriteLine(
					string.Format(" --> {0} {1}: {2} {3} {5} bytes {4}",
						response.StatusCode, response.StatusDescription, response.CharacterSet,
						response.ContentEncoding, response.ContentType, response.ContentLength))
			};
			JsConfig<Artefact>.SerializeFn = a => a.Data.ToJson();//.SerializeToString();	//a.ToBsonDocument();
			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { Data = a.FromJson<DataDictionary>() };		
//			JsConfig<Artefact>.SerializeFn = a => new DynamicJson(a.Data); StringExtensions.ToJson<DataDictionary>(a.Data);	// a.Data.ToJson();	// TypeSerializer.SerializeToString<DataDictionary>(a.Data);	// a.Data.SerializeToString();
//			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { /*Persisted*/Data = a.FromJson<DataDictionary>() };	// TypeSerializer.DeserializeFromString<DataDictionary>(a) };//.FromJson<DataDictionary>() };
//			JsConfig<Expression>.SerializeFn = e => new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer()).SerializeText(e);
//			JsConfig<QueryDocument>.SerializeFn = q => StringExtensions.ToJsv<QueryDocument>(q);//q.ToJson(); //((BsonDocument)q).AsByteArray;
//			JsConfig<QueryDocument>.DeSerializeFn = q => q.FromJsv<QueryDocument>();
//			JsConfig<IMongoQuery>.SerializeFn = q => StringExtensions.ToJsv<IMongoQuery>(q);//q.ToJson(); //((BsonDocument)q).AsByteArray;
//			JsConfig<IMongoQuery>.DeSerializeFn = q => q.FromJsv<QueryDocument>();
			bufferWriter.Write("OK\n");
		}
		
		#region Methods relating arbitrary data instances to their associated Artefacts
		/// <summary>
		/// Determines whether this instance has an associated <see cref="Artefact"/> 
		/// </summary>
		/// <returns><c>true</c> if the <paramref name="instance"/> has an associated <see cref="Artefact"/>, otherwise <c>false</c></returns>
		/// <param name="instance">Instance.</param>
		public bool HasArtefact(object instance)
		{
			return _artefacts.ContainsKey(instance);
		}
		
		/// <summary>
		/// Return an <see cref="Artefact"/> associated with the <paramref name="instance"/>
		/// Throws an exception if not found.
		/// </summary>
		/// <returns><see cref="Artefact"/> associated with the <paramref name="instance"/></returns>
		/// <param name="instance">Instance.</param>
		public Artefact GetArtefact(object instance)
		{
			return _artefacts[instance];
		}
		
		/// <summary>
		/// Tries to Return an <see cref="Artefact"/> associated with the <paramref name="instance"/>
		/// </summary>
		/// <returns><c>true</c>, if an artefact was found, <c>false</c> otherwise.</returns>
		/// <param name="instance">Instance.</param>
		/// <param name="artefact">A variable to receive the <see cref="Artefact"/> if found, or <c>null</c> otherwise</param>
		public bool TryGetArtefact(object instance, out Artefact artefact)
		{
			return _artefacts.TryGetValue(instance, out artefact);
		}
		
		/// <summary>
		/// Gets the <see cref="Artefact"/> associated with this instance, or creates a new one
		/// Note creatinga  new <see cref="Artefact"/> instance does not create one on the server necessarily,
		/// it may use the new instance to retrieve pre-existing data.
		/// </summary>
		/// <returns>The pre-existing or newly created artefact.</returns>
		/// <param name="instance">Instance.</param>
		public Artefact GetOrCreateArtefact(object instance)
		{
			if (!_artefacts.ContainsKey(instance))
			{
				Artefact newArtefact = new Artefact(instance, this);
				_artefacts[instance] = newArtefact;
			}
			return _artefacts[instance];
		}
		#endregion

		/// <summary>
		/// Get the specified match.
		/// </summary>
		/// <param name="match">Match.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public QueryResults Get<T>(Expression<Func<T, bool>> predicate)
		{
			predicate = (Expression<Func<T, bool>>)Visitor.Visit(predicate);
			_bufferWriter.WriteLine("Get<{0}>(match: {1})", typeof(T).FullName, predicate);
			QueryResults results = _serviceClient.Get<QueryResults>(QueryRequest<T>.Make(predicate));
			_bufferWriter.WriteLine("result = " + results);
			return results;
		}
		
		/// <summary>
		/// Gets or creates an artefact 
		/// Haven't decided return type yet (SS DTO? Some sanitised/interpreted result based on DTO obtained in this method?)
		/// </summary>
		/// <param name="identify">Identify.</param>
		/// <param name="create">Create.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		/// <remarks>
		/// TODO: Consider if create param delegate should really be named pushInstance or something? Because it might not
		/// have to be a creation delegate. It might just have an instance already (like I think ArtefactTestClient does)
		/// to push to server if an equivalent does not already exist
		/// Is "Put()" the right HTTP verb I should be using for this method? ie "put this at [uri]", whether it exists or not
		/// as opposed to "Post()" which *might* (check!:)) mean "Post this new object [optionally at uri] and
		/// error out if exists"???
		/// </remarks>
		public T GetOrCreate<T>(Expression<Func<T, bool>> predicate, Func<T> create)
		{
			if (create == null)
				throw new ArgumentNullException("create");
			if (predicate == null)
				throw new ArgumentNullException("match");
			predicate = (Expression<Func<T, bool>>)Visitor.Visit(predicate);
			_bufferWriter.WriteLine("GetOrCreate<{0}>(match: {1}, create: {2})", typeof(T).FullName, predicate, create);
			Artefact artefact;
			QueryRequest<T> query = QueryRequest<T>.Make(predicate);
			QueryResults result = _serviceClient.Get<QueryResults>(query);
			_bufferWriter.WriteLine("result = " + result);
			if (result == null || result.Artefacts.Count() == 0)
			{
				artefact = new Artefact(create(), this);
				_serviceClient.Post(artefact);
			}
			else
			{
				artefact = result.Artefacts.ElementAt(0);
			}
			T instance = artefact.As<T>();
			_artefacts[instance] = artefact;
			return instance;
		}		
		
		/// <summary>
		/// Updates or creates an artefact
		/// </summary>
		/// <param name="match">Matching lambda</param>
		/// <param name="create">Create.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		/// <returns><c>true</c> if the artefact was newly created, otherwise, <c>false</c></returns>
		public bool Save<T>(Expression<Func<T, bool>> match, T instance)
			// might be avalue type?
//			if (instance == null)
//				throw new ArgumentNullException("create");
			if (match == null)
				throw new ArgumentNullException("match");
			match = (Expression<Func<T, bool>>)Visitor.Visit(match);
			_bufferWriter.WriteLine("Save<{0}>(match: {1}, instance: {2})", typeof(T).FullName, match, instance);
			QueryRequest query = QueryRequest.Make<T>(match);
			Artefact artefact;	// = _serviceClient.Get<Artefact>(query);
			QueryResults result = _serviceClient.Get<QueryResults>(query);
			_bufferWriter.WriteLine("result = " + result);
			if (result == null || result.Artefacts.Count() == 0)
			{
				artefact = new Artefact(instance, this) {
					Collection = typeof(T).Name		// TODO: <-- ? Manually use T.name in URL which becomes the collection name on server side
				};
				_serviceClient.Post(artefact);
				_artefacts[instance] = artefact;
				return true;
			}
			else
			{
				artefact = result.Artefacts.ElementAt(0);
				artefact.SetInstance(instance);
				_serviceClient.Put(artefact);
				return false;
			}
		}
	}
}


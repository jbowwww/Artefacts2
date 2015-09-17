using System;
using ServiceStack;
using System.Net;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Artefacts.Service
{
	public class ArtefactsClient
	{
		#region Private fields
		private TextBufferWriter _bufferWriter;

		private string _serviceBaseUrl;

		private IServiceClient _serviceClient;
		
		private readonly Dictionary<object, Artefact> _artefacts = new Dictionary<object, Artefact>();
		#endregion
		
		public ArtefactsClient(string serviceBaseUrl, TextBufferWriter bufferWriter)
		{
			//			_client = client;
			_bufferWriter = bufferWriter;
			_serviceBaseUrl = serviceBaseUrl;
			_bufferWriter.Write(string.Format("Creating client to access {0} ... ", _serviceBaseUrl));
			_serviceClient = new ServiceStack.JsonServiceClient(_serviceBaseUrl) {
				RequestFilter = (HttpWebRequest request) => bufferWriter.Write(
					string.Format("\nClient.{0} HTTP {6} {5} {2} bytes {1} Expect {7} Accept {8}\n",
				              request.Method, request.ContentType,  request.ContentLength,
				              request.UserAgent, request.MediaType, request.RequestUri,
				              request.ProtocolVersion, request.Expect, request.Accept)),
				ResponseFilter = (HttpWebResponse response) => bufferWriter.Write(
					string.Format(" --> {0} {1}: {2} {3} {5} bytes {4}: {6}\n",
				              response.StatusCode, response.StatusDescription, response.CharacterSet,
				              response.ContentEncoding, response.ContentType, response.ContentLength,
				              response.ReadToEnd())) };
//			JsConfig<Expression>.SerializeFn = e => new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer()).SerializeText(e);
			
			bufferWriter.Write("OK\n");
		}
		
<<<<<<< HEAD
		#region Methods
		#region Sync
=======
		/// Function that checks if an instance is equivalent 
		/// This was initial idea but better to just use IEquatable.Equals??
		public delegate bool Identify<T>(T instance);
		
		// Use this or constructor/initialiser, ???
		public delegate T Create<T>();
		
		/// <summary>
		/// Gets or creates an artefact 
		/// Haven't decided return type yet (SS DTO? Some sanitised/interpreted result based on DTO obtained in this method?)
		/// </summary>
		/// <param name="identify">Identify.</param>
		/// <param name="create">Create.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
//		public T Sync<T>(Identify<T> identify, Create<T> create)
//		{
//			return default(T);	
//		}
		
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
		/// <summary>
		/// Gets or creates an artefact 
		/// Haven't decided return type yet (SS DTO? Some sanitised/interpreted result based on DTO obtained in this method?)
		/// </summary>
		/// <param name="identify">Identify.</param>
		/// <param name="create">Create.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T Sync<T>(Expression<Func<T, bool>> match, Expression<Func<T>> create)
		{
<<<<<<< HEAD
			using (var group = _bufferWriter.WriteLine("Sync<{0}>(match: {1}, create: {2})", typeof(T).FullName, match.ToString(), create.ToString()))
			{
				string collectionName = typeof(T).FullName;
				MatchArtefactRequest request = new MatchArtefactRequest() { Match = new ClientQueryVisitor().Visit(match) };
				Artefact artefact =
				_serviceClient.Get<Artefact>(request) ??
					new Artefact(create != null ? create() : default(T)) {
					Collection = typeof(T).Name		// TODO: <-- ? Manually use T.name in URL which becomes the collection name on server side
				};
				if (artefact.State == ArtefactState.Created)
					_serviceClient.Put(artefact);
				T instance = artefact.As<T>();
				_artefacts[instance] = artefact;
				return instance;
			}
=======
			MatchArtefactRequest request = new MatchArtefactRequest() { Match = new ClientQueryVisitor().Visit(match) };
			Artefact repositoryArtefact = _serviceClient.Get<Artefact>(request);
			
			return default(T);
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
		}
		
		/// <summary>
		/// Gets or creates an artefact 
		/// Haven't decided return type yet (SS DTO? Some sanitised/interpreted result based on DTO obtained in this method?)
		/// </summary>
		/// <param name="identify">Identify.</param>
		/// <param name="create">Create.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		/// <returns>
		/// <c>true</c> if the artefact existed (according to <see cref="IEquatable<T>.Equals"/> and was retrieved.
		/// The <paramref name="artefact"/> will have had its properties updated to reflect the retrieved data.
		/// </returns>
		public bool Sync<T>(T instance)
		{
			using (var group = _bufferWriter.WriteLine("Sync<{0}>(instance: {1})", typeof(T).FullName, instance.ToString()))
			{
				Artefact artefact = GetArtefact(instance);
				return false;
			}
		}
		
		/// <summary>
		/// Gets or creates an artefact 
		/// Haven't decided return type yet (SS DTO? Some sanitised/interpreted result based on DTO obtained in this method?)
		/// </summary>
		/// <param name="identify">Identify.</param>
		/// <param name="create">Create.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public object Sync(object artefact)
		{
			using (var group = _bufferWriter.WriteLine("Sync<{0}>(artefact: {1})", artefact.ToString()))
			{
				return null;
			}
		}
		
		/// <summary>
		/// Gets or creates an artefact 
		/// Haven't decided return type yet (SS DTO? Some sanitised/interpreted result based on DTO obtained in this method?)
		/// </summary>
		/// <param name="identify">Identify.</param>
		/// <param name="create">Create.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
//		public T Sync<T>(Create<T> create)
//		{
//			
//		}
		#endregion	// Sync
		
		#region GetOrCreate
		public Artefact GetOrCreate<T>(string name)
		{
			using (var group = _bufferWriter.StartGroup("GetOrCreate<{0}>(name: \"{1}\")", typeof(T).FullName, name))
			{
				string collectionName = typeof(T).FullName;
				string uri = Path.Combine(collectionName, name);
				Artefact artefact = _serviceClient.Get<Artefact>(request);
				T instance;
				if (artefact != null)
					instance = artefact.As<T>();
				else
				{
					instance = create != null ? create() : default(T);
					artefact = new Artefact(instance) { Collection = typeof(T).Name		// TODO: <-- ? Manually use T.name in URL which becomes the collection name on server side
					};
					_serviceClient.Post(artefact);
				}
			}
		}
			
//			public ArtefactContext GetOrCreate<T>(Func<T, bool> match, Func<T> create)
//			{
//				_bufferWriter.WriteLine("Sync<{0}>(match: {1}, create: {2})", typeof(T).FullName, match.ToString(), create.ToString());
//				string collectionName = typeof(T).FullName;
//				MatchArtefactRequest request = new MatchArtefactRequest() { Match = new ClientQueryVisitor().Visit(match) };
//				Artefact artefact = _serviceClient.Get<Artefact>(request);
//				T instance;
//				if (artefact != null)
//					instance = artefact.As<T>();
//				else
//				{
//					instance = create != null ? create() : default(T);
//					artefact = new Artefact(instance) { Collection = typeof(T).Name		// TODO: <-- ? Manually use T.name in URL which becomes the collection name on server side
//					};
//					_serviceClient.Post(artefact);
//
//				}

		#endregion
		
		#region Misc
		/// <summary>
		/// Gets the <see cref="Artefact"/> associated with this instance
		/// </summary>
		/// <returns>The artefact.</returns>
		/// <param name="instance">Instance.</param>
		public Artefact GetArtefact(object instance)
		{
			return _artefacts.ContainsKey(instance) ?
				_artefacts[instance]
			:	(_artefacts[instance] = new Artefact(instance));
		}
		#endregion // Misc
		#endregion // Methods
	}
}


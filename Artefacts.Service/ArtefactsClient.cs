using System;
using ServiceStack;
using System.Net;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using ServiceStack.Text;

namespace Artefacts.Service
{
	public class ArtefactsClient
	{
		private TextWriter _bufferWriter;

		private string _serviceBaseUrl;

		private IServiceClient _serviceClient;
		
		
		private readonly Dictionary<object, Artefact> _artefacts = new Dictionary<object, Artefact>();
		
		public ArtefactsClient(string serviceBaseUrl, TextWriter bufferWriter)
		{
			//			_client = client;
			_bufferWriter = bufferWriter;
			_serviceBaseUrl = serviceBaseUrl;
			_bufferWriter.Write(string.Format("Creating client to access {0} ... ", _serviceBaseUrl));
			_serviceClient = new ServiceStack.JsonServiceClient(_serviceBaseUrl) {
				RequestFilter = (HttpWebRequest request) => {
//					Stream bStream = new BufferedStream(request.GetRequestStream());
					bufferWriter.Write(
						string.Format("\nClient.{0} HTTP {6} {5} {2} bytes {1} Expect {7} Accept {8}\n{9}\n",
				              request.Method, request.ContentType,  request.ContentLength,
				              request.UserAgent, request.MediaType, request.RequestUri,
				              request.ProtocolVersion, request.Expect, request.Accept,
						string.Format("\tHeaders: {0}", request.Headers.ToString()),
						request.Method == "GET" ? string.Empty : string.Format("\tBody: {0}", request.ToString())));//request.GetRequestStream()..ReadLines().Join("\n"))
				/* "--not implemented--" /*bStream.ReadLines().Join("\n")*/// ));
				},
//				              request.ContentLength != 0 ? string.Empty
//				              : string.Format("\tBody: {0}", request.GetRequestStream().ReadAsync(.ToString()))),
				ResponseFilter = (HttpWebResponse response) => bufferWriter.Write(
					string.Format(" --> {0} {1}: {2} {3} {5} bytes {4}: {6}\n",
				              response.StatusCode, response.StatusDescription, response.CharacterSet,
				              response.ContentEncoding, response.ContentType, response.ContentLength,
				              response.ReadToEnd())) };
//			JsConfig<Expression>.SerializeFn = e => new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer()).SerializeText(e);
			JsConfig<Artefact>.SerializeFn = a => a.Data.SerializeToString();	//a.ToBsonDocument();
			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { Data = a.FromJson<DataDictionary>() };
			bufferWriter.Write("OK\n");
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
		public T Sync<T>(Expression<Func<T, bool>> match, Func<T> create) where T : new()
		{
			_bufferWriter.WriteLine("Sync<{0}>(match: {1}, create: {2})", typeof(T).FullName, match.ToString(), create.ToString());

			if (create == null)
				throw new ArgumentNullException("create");
			if (match == null)
				throw new ArgumentNullException("match");
			
			string collectionName = typeof(T).FullName;
			MatchArtefactRequest request = new MatchArtefactRequest(match);
			Artefact artefact = _serviceClient.Get<Artefact>(request);
			if (artefact == null)
				artefact = new Artefact(create != null ? create() : default(T), this) {
					Collection = typeof(T).Name		// TODO: <-- ? Manually use T.name in URL which becomes the collection name on server side
				};
			if (artefact.State == ArtefactState.Created)
				_serviceClient.Put(artefact);
			T instance = artefact.As<T>();
			_artefacts[instance] = artefact;
			return instance;
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
			Artefact artefact = GetArtefact(instance);
			
			return false;
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

			return null;
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
		
		/// <summary>
		/// Gets the <see cref="Artefact"/> associated with this instance
		/// </summary>
		/// <returns>The artefact.</returns>
		/// <param name="instance">Instance.</param>
		public Artefact GetArtefact(object instance)
		{
			return _artefacts.ContainsKey(instance) ?
				_artefacts[instance]
			:	(_artefacts[instance] = new Artefact(instance, this));
		}
	}
}


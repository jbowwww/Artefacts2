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
		
		/// <summary>
		/// Gets or creates an artefact 
		/// Haven't decided return type yet (SS DTO? Some sanitised/interpreted result based on DTO obtained in this method?)
		/// </summary>
		/// <param name="identify">Identify.</param>
		/// <param name="create">Create.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T Sync<T>(Expression<Func<T, bool>> match, Expression<Func<T>> create)
		{
			MatchArtefactRequest request = new MatchArtefactRequest() { Match = new ClientQueryVisitor().Visit(match) };
			Artefact repositoryArtefact = _serviceClient.Get<Artefact>(request);
			
			return default(T);
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
			:	(_artefacts[instance] = new Artefact(instance));
		}
	}
}


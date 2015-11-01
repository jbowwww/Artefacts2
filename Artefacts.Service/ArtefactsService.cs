using System;
using Artefacts;
using MongoDB;
using MongoDB.Bson;
using System.IO;
using System.Web;
using System.Net;
using ServiceStack;
using System.Runtime.CompilerServices;
using ServiceStack.Text;
using System.Linq.Expressions;
using Serialize.Linq.Nodes;
using Serialize.Linq.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using System.Linq;
using System.Collections.Generic;
using Artefacts.FileSystem;
using System.Collections;
using System.Reflection;
using ServiceStack.Logging;

namespace Artefacts.Service
{
	/// <summary>
	/// Artefacts service.
	/// </summary>
	public class ArtefactsService : ServiceStack.Service
	{
		#region Static members
		static readonly ILog Log;
		
		/// <summary>
		/// Initializes the <see cref="Artefacts.Service.ArtefactsService"/> class.
		/// </summary>
		static ArtefactsService()
		{
			Log = ArtefactsHost.LogFactory.GetLogger(typeof(ArtefactsService));
		}
		#endregion
		
		#region Private fields
		private TextWriter _output;
		
		// TODO: Refactor out to a new storage class , keep it generic enough for possible storage provider changes?
		private MongoClient _mClient;
		private MongoClientSettings _mClientSettings;
		private MongoDatabase _mDb;
		private MongoCollection/*<Artefact>*/ _mcArtefacts;
		#endregion
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Service.ArtefactsService"/> class.
		/// </summary>
		/// <param name="output">Output.</param>
		public ArtefactsService(TextWriter output)
		{
			Log.DebugFormat("ArtefactsService({0})", output);
			// Debug
			_output = output;
			
			Log.Info("Starting service");
			// ServiceStack setup
			
//			JsConfig<Expression>.DeSerializeFn = s => new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer()).DeserializeText(s);
			JsConfig<Artefact>.SerializeFn = a => StringExtensions.ToJsv<DataDictionary>(a./*Persisted*/Data);	// a.Data.ToJson();	// TypeSerializer.SerializeToString<DataDictionary>(a.Data);	// a.Data.SerializeToString();
			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { Data = a.FromJsv<DataDictionary>() };	// TypeSerializer.DeserializeFromString<DataDictionary>(a) };//.FromJson<DataDictionary>() };
//			JsConfig<QueryDocument>.SerializeFn = q => q.ToJsv(); //((BsonDocument)q).AsByteArray;
//			JsConfig<QueryDocument>.DeSerializeFn = q => q.FromJsv<QueryDocument>();
//			JsConfig<IMongoQuery>.SerializeFn = q => q.ToJsv(); //((BsonDocument)q).AsByteArray;
//			JsConfig<IMongoQuery>.DeSerializeFn = q => q.FromJsv<QueryDocument>();
			
			// Storage (Mongo) setup
			_mClientSettings = new MongoClientSettings() { };		// TODO: Settings
			_mClient = new MongoClient("server=localhost");
			Log.Debug(_mClient);
			_output.WriteLine("mClient: " + _mClient.ToString());//.FormatString(3, "  "));//.Dump());
			_mDb = new MongoDatabase(_mClient.GetServer(), "Artefacts", new MongoDatabaseSettings());
			Log.Debug(_mDb);
			_mcArtefacts = _mDb.GetCollection/*<Artefact>*/("Artefacts");
			Log.Debug(_mcArtefacts);
		}

		/// <summary>
		/// Put the specified artefact.
		/// </summary>
		/// <param name="artefact">Artefact.</param>
		/// <remarks>
		/// TODO: Look at inserting into Mongo using JSON received as service?
		/// </remarks>
		public object Post(Artefact artefact)
		{
			try
			{
				Log.DebugFormat("Put({0})", artefact);
				_output.WriteLine("Artefact artefact: " + artefact.ToString());
				WriteConcernResult result = _mcArtefacts.Insert<BsonDocument>(BsonDocument.Create(artefact.Data));
				Log.Debug(result);
				if (result.Ok)
					artefact.State = ArtefactState.Current;
				return result.ToString();
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				_output.WriteLine(ex.ToString());
				throw;
				return string.Empty;
			}
		}
		
		/// <summary>
		/// Get the specified request.
		/// </summary>
		/// <param name="request">Request.</param>
		public Artefact Get(MatchArtefactRequest request)
		{
			try {
				
				//base.Request.InputStream;
				Log.DebugFormat("Get({0})", request);
				_output.WriteLine("{0}: {1}", request.Match.GetType(), ExpressionPrettyPrinter.PrettyPrint(request.Match));
				
				object result = _mcArtefacts.FindAs<Artefact>(Query<Disk>.Where((Expression<Func<Disk, bool>>)request.Match));
				Log.Debug(result);
				List<object> results = new List<object>();
				foreach (object r in (IEnumerable)result)
				{
					results.Add(r);
					Log.Debug(r);
					_output.WriteLine(r.ToString());
				}
				return results.Count > 0 ? (Artefact)results[0] : null;
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				_output.WriteLine(ex.ToString());
				throw;
				return null;
			}
		}
		
		/// <summary>
		/// Get the specified query.
		/// </summary>
		/// <param name="query">Query.</param>
		public QueryResults Get(QueryRequest query)
		{
			try {
				Log.DebugFormat("Get({0})", query);
				_output.WriteLine("{0}: {1}", query.Where.GetType(), ExpressionPrettyPrinter.PrettyPrint(query.Where));
				Type elementType = query.Where.Parameters[0].Type;
				Type filterFunc = typeof(Func<>).MakeGenericType(elementType);
				Type queryType = typeof(Query<>).MakeGenericType(elementType);
				MethodInfo whereMethod = queryType.GetMethod("Where");
				IMongoQuery mongoQuery = (IMongoQuery)whereMethod.Invoke(null, new object[] { query.Where });
				object result = _mcArtefacts.FindAs<Artefact>(mongoQuery);
				Log.Debug(result);
				List<Artefact> results = ((IEnumerable<Artefact>)result).ToList();
				QueryResults r = new QueryResults(results);
				Log.Debug(r);
				return r;
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				_output.WriteLine(ex.ToString());
				throw;
				return null;
			}
		}
		
		/// <summary>
		/// Any the specified request.
		/// </summary>
		/// <param name="request">Request.</param>
		public object Any(object request)
		{
			try
			{
				Log.DebugFormat("Any({0})", request);
				_output.WriteLine("Any ! request: " + request.ToString());
				return default(HttpWebResponse);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				_output.WriteLine(ex.ToString());
				throw;
				return null;
			}
			return null;
		}
	}
}


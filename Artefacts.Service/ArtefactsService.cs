using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Collections.Generic;
using Artefacts;
using ServiceStack.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using ServiceStack;
using ServiceStack.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using System.Linq.Expressions;

namespace Artefacts.Service
{
	/// <summary>
	/// Artefacts service.
	/// </summary>
	public class ArtefactsService : ServiceStack.Service		//, IDebugTimerTarget
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
		
		/// <summary>
		/// Makes the name of the safe collection.
		/// </summary>
		/// <returns>The safe collection name.</returns>
		/// <param name="collectionName">Collection name.</param>
		public static string MakeSafeCollectionName(string collectionName)
		{
			return collectionName.Replace(".", "").Replace("`", "-").Replace('[', '-').Replace(']', '-');
		}
		#endregion
		
		enum SaveType {
			Insert,
			Update,
			InsertOrUpdate
		};
		
		#region Private fields
		private TextWriter _output;
		private MongoClient _mClient;		// TODO: Refactor out to a new storage class , keep it generic enough for possible storage provider changes?
		private MongoClientSettings _mClientSettings;
		private MongoDatabase _mDb;
		private MongoCollection<Artefact> _mcArtefacts;
		private ServerQueryVisitor _visitor;
		public Dictionary<string, Artefact> _artefactCache;
		private MongoDB.Bson.IO.JsonWriterSettings _jsonSettings =
			new MongoDB.Bson.IO.JsonWriterSettings()
		{
			OutputMode = MongoDB.Bson.IO.JsonOutputMode.Strict
		};
		private MongoDB.Bson.Serialization.IBsonSerializationOptions _serializationOptions =
			MongoDB.Bson.Serialization.Options.DictionarySerializationOptions.Document;
		#endregion
		
		#region Properties
		/// <summary>
		/// Gets the visitor.
		/// </summary>
		public ServerQueryVisitor Visitor {
			get { return _visitor ?? (_visitor = new ServerQueryVisitor()); }
		}
		
		/// <summary>
		/// Gets the artefact cache.
		/// </summary>
		public Dictionary<string, Artefact> ArtefactCache {
			get { return _artefactCache ?? (_artefactCache = new Dictionary<string, Artefact>()); }
		}
		#endregion
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Service.ArtefactsService"/> class.
		/// </summary>
		/// <param name="output">Output.</param>
		public ArtefactsService(TextWriter output)
		{
//			HostContext.DebugMode = true;
			HostContext.Config.ReturnsInnerException = true;
			HostContext.Config.WriteErrorsToResponse = true;
//			DebugTimer.Created(this);
			_output = output;
			Log.DebugFormat("ArtefactsService({0})", output);
			Log.Info("Starting service");
			// Storage (Mongo) setup
			_mClientSettings = new MongoClientSettings() { };		// TODO: Settings
			_mClient = new MongoClient("server=localhost");
			Log.Debug(_mClient);
			_output.WriteLine("mClient: " + _mClient);
			_mDb = new MongoDatabase(_mClient.GetServer(), "Artefacts", new MongoDatabaseSettings());
			Log.Debug(_mDb);
			_mcArtefacts = _mDb.GetCollection<Artefact>("Artefacts");
			Log.Debug(_mcArtefacts);
			Artefact.ConfigureServiceStack();
//			ServiceStackHost.Instance.RequestBinders.Add(typeof(QueryRequest), (request) => Get((QueryRequest)request.Dto));
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
//			using (new DebugTimer(this, &TotalTimePost))
			//			{
			try {
				Log.Debug("HTTP POST: " + artefact);
				_output.WriteLine("HTTP POST: " + artefact);
				ArtefactCache[artefact.Id] = artefact;
				WriteConcernResult result = Save(artefact);
				Log.Debug("Save(artefact,SaveType.InsertOrUpdate): " + result.Response.ToString());
				_output.WriteLine("Save(artefact,SaveType.InsertOrUpdate): " + result.Response.ToString());
				object r = default(HttpWebResponse);// artefact;
//				return r;
				return r;
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				_output.WriteLine(ex.ToString());
				throw;
			}
//			}
		}
		
		/// <summary>
		/// Put the specified artefact.
		/// </summary>
		/// <param name="artefact">Artefact.</param>
		public object Put(Artefact artefact)
		{
//			using (new DebugTimer(this, &TotalTimePost))
//			{
			try {
				Log.DebugFormat("HTTP PUT: ", artefact);
				_output.WriteLine("HTTP PUT: " + artefact);
				ArtefactCache[artefact.Id] = artefact;
				WriteConcernResult result = Save(artefact, SaveType.InsertOrUpdate);	//.Update);	//InsertOrUpdate(artefact);
				Log.Debug("Save(artefact,SaveType.InsertOrUpdate): " + result.Response.ToString());
				_output.WriteLine("Save(artefact,SaveType.InsertOrUpdate): " + result.Response.ToString());
				object r = default(HttpWebResponse);// artefact;
				return r;
				//return null;
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				_output.WriteLine(ex.ToString());
				throw;
			}
//			
//			}
		}
		
		public object Post(SaveRequest save)
		{
			MongoCollection<Artefact> _mcArtefacts = _mDb.GetCollection<Artefact>(save.CollectionName);
			BsonDocument artefactData = BsonDocument.Create(save.Data);
			object result =
				save.Type == SaveRequestType.Insert ? _mcArtefacts.Insert<BsonDocument>(artefactData)
			:	save.Type == SaveRequestType.Update ? _mcArtefacts.Update(Query.EQ("_id", BsonString.Create(save.Data["_id"])), Update<BsonDocument>.Replace(artefactData))
			:	save.Type == SaveRequestType.Upsert ? _mcArtefacts.Save(artefactData)
			:	default(WriteConcernResult);
//			if (result.Ok)
//				artefact.State = ArtefactState.Current;
			return default(HttpWebResponse);
		}
		
		public object Post(SaveBatchRequest saveBatch)
		{
			List<object> results = new List<object>();
			foreach (SaveRequest save in saveBatch.Items)
			{
				MongoCollection<Artefact> _mcArtefacts = _mDb.GetCollection<Artefact>(save.CollectionName);
				BsonDocument artefactData = BsonDocument.Create(save.Data);
				object result =
					save.Type == SaveRequestType.Insert ? _mcArtefacts.Insert<BsonDocument>(artefactData)
				:	save.Type == SaveRequestType.Update ? _mcArtefacts.Update(Query.EQ("_id", BsonString.Create(save.Data["_id"])), Update<BsonDocument>.Replace(artefactData))
				:	save.Type == SaveRequestType.Upsert ? _mcArtefacts.Save(artefactData)
				:	default(WriteConcernResult);
				//			if (result.Ok)
				//				artefact.State = ArtefactState.Current;
				results.Add(result);
			}
			return default(HttpWebResponse);	//results.ToArray();
		}
		
		/// <summary>
		/// Get the specified query.
		/// </summary>
		/// <param name="query">Query.</param>
		public /* object */ QueryResults Get(QueryRequest query) 	//object queryObject)
		{
			Log.Debug("HTTP GET: " + query);
			_output.WriteLine("HTTP GET: " + query);
			
			MongoCollection<Artefact> _mcQueryCollection = _mDb.GetCollection<Artefact>(query.CollectionName);
			QueryResults queryResult;
//			if (query.Operation == "Count")
//			{
//				long resultCount = (query.Query == Query.Null) ? _mcQueryCollection.Count() : _mcQueryCollection.Count(query.Query);
//				queryResult = new QueryResults((int)resultCount);
//				Log.DebugFormat("{0}.Count({1}): {2}", query.CollectionName, query.Query == null ? "" : query.Query.ToString(), queryResult);
//				_output.WriteLine("{0}.Count({1}): {2}", query.CollectionName, query.Query == null ? "" : query.Query.ToString(), queryResult);
//			}
//			else if (query.Operation == "Where")
//				{
					MongoCursor<Artefact> _mcQueryResults = _mcQueryCollection.Find(query.Query);
					queryResult = new QueryResults(_mcQueryResults.ToList());
					Log.DebugFormat("{0}.Find({1}): {2}", query.CollectionName, query.Query == null ? "" : query.Query.ToString(), queryResult);
					_output.WriteLine("{0}.Find({1}): {2}", query.CollectionName, query.Query == null ? "" : query.Query.ToString(), queryResult);
//				}
//				else
//					throw new InvalidOperationException(string.Format("Invalid operation \"{0}\" for request {1}", query.Operation, query));
			return queryResult;
		}
		
		public CountResults Get(CountRequest query)
		{
			Log.Debug("HTTP GET: " + query);
			_output.WriteLine("HTTP GET: " + query);

			MongoCollection<Artefact> _mcQueryCollection = _mDb.GetCollection<Artefact>(query.CollectionName);
			CountResults queryResult;
			//			if (query.Operation == "Count")
			//			{
			//				long resultCount = (query.Query == Query.Null) ? _mcQueryCollection.Count() : _mcQueryCollection.Count(query.Query);
			//				queryResult = new QueryResults((int)resultCount);
			//				Log.DebugFormat("{0}.Count({1}): {2}", query.CollectionName, query.Query == null ? "" : query.Query.ToString(), queryResult);
			//				_output.WriteLine("{0}.Count({1}): {2}", query.CollectionName, query.Query == null ? "" : query.Query.ToString(), queryResult);
			//			}
			//			else if (query.Operation == "Where")
			//				{
			long count = _mcQueryCollection.Count(query.Query);
			queryResult = new CountResults() { Count = (int)count };
			Log.DebugFormat("{0}.Find({1}): {2}", query.CollectionName, query.Query == null ? "" : query.Query.ToString(), queryResult);
			_output.WriteLine("{0}.Find({1}): {2}", query.CollectionName, query.Query == null ? "" : query.Query.ToString(), queryResult);
			//				}
			//				else
			//					throw new InvalidOperationException(string.Format("Invalid operation \"{0}\" for request {1}", query.Operation, query));
			return queryResult;
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
			}
			//return null;
		}
		
		/// <summary>
		/// Save the specified artefact and saveType.
		/// </summary>
		/// <param name="artefact">Artefact.</param>
		/// <param name="saveType">Save type.</param>
		private WriteConcernResult Save(Artefact artefact, SaveType saveType = SaveType.InsertOrUpdate)
		{
//			try
//			{
				MongoCollection<Artefact> _mcArtefacts = _mDb.GetCollection<Artefact>(artefact.Collection);
				BsonDocument artefactData = BsonDocument.Create(artefact.Data);
			
				WriteConcernResult result =
					saveType == SaveType.Insert ? _mcArtefacts.Insert<BsonDocument>(artefactData)
				:	saveType == SaveType.Update ? _mcArtefacts.Update(
						Query.EQ("_id", BsonString.Create(artefact.Id)),
//						Query<BsonDocument>.EQ<string>(a => (string)a["_id"], artefact.Id),
						Update<BsonDocument>.Replace(artefactData))
				:	saveType == SaveType.InsertOrUpdate ? _mcArtefacts.Save(artefactData)// _mcArtefacts.Save<BsonDocument>(artefactData)
				: default(WriteConcernResult);
				if (result.Ok)
					artefact.State = ArtefactState.Current;
				return result;
//			}
//			catch (Exception ex)
//			{
//				Log.Error(ex);
//				_output.WriteLine(ex.ToString());
//				throw;
//			}
		}
	}
}


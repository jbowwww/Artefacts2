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
		private ExpressionVisitor _visitor;
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
		public ExpressionVisitor Visitor {
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
				Log.Debug("Save(artefact,SaveType.InsertOrUpdate): " + result);
				_output.WriteLine("Save(artefact,SaveType.InsertOrUpdate): " + result);
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
				WriteConcernResult result = Save(artefact, SaveType.Update);	//InsertOrUpdate(artefact);
				Log.Debug("Save(artefact,SaveType.Update): " + result);
				_output.WriteLine("Save(artefact,SaveType.Update): " + result);
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
		
		/// <summary>
		/// Get the specified query.
		/// </summary>
		/// <param name="query">Query.</param>
		public QueryResults Get(QueryRequest query)
		{
//			try {
//				using (new DebugTimer(this, &TotalTimeGet))
//				{
					Log.Debug("HTTP GET: " + query);
					_output.WriteLine("HTTP GET: " + query);
	//				_output.WriteLine("{0}: {1}", query.Predicate.GetType(), ExpressionPrettyPrinter.PrettyPrint(query.Predicate));
	//				Type elementType = query.Predicate.Parameters[0].Type;
	//				Type queryType = typeof(Query<>).MakeGenericType(elementType);
	//				MethodInfo whereMethod = queryType.GetMethod("Where");
	//				LambdaExpression predicate = (LambdaExpression)Visitor.Visit(query.Predicate);
	//				IMongoQuery mongoQuery = (IMongoQuery)whereMethod.Invoke(null, new object[] { predicate });
			MongoCollection<BsonDocument/*Artefact*/> _mcQueryCollection = _mDb.GetCollection<BsonDocument>(query.CollectionName);	// elementType.Name);
			MongoCursor<Artefact> mongoQueryResult = _mcQueryCollection.FindAs<Artefact>(query.Query);
			Log.Debug("_mcArtefacts.FindAs<Artefact>(query): " + mongoQueryResult);
			_output.WriteLine("_mcArtefacts.FindAs<Artefact>(query): " + mongoQueryResult);
//					List<Artefact> results = mongoQueryResult.ToList();
//				result != null ? result.ToList() : new List<Artefact>();
//						((IEnumerable<Artefact>)result).ToList() :
//						new List<Artefact>();
//					QueryResults r = new QueryResults(results);
			QueryResults queryResult = new QueryResults();//mongoQueryResult);
			foreach (Artefact artefact/*Data*/ in mongoQueryResult)
			{
//				string artefactId = artefactData["_id"].AsString;
//				DataDictionary data = new DataDictionary(artefactData.ToDictionary());
//				Artefact artefact = new Artefact(data);		//new DataDictionary(artefactData));
				ArtefactCache[artefact.Id] = artefact;
				queryResult.Artefacts.Add(artefact);
			}
					Log.Debug("new QueryResults(): " + queryResult);
					_output.WriteLine("new QueryResults(): " + queryResult);
					return queryResult;
//				}
//			}
//			catch (Exception ex)
//			{
//				Log.Error(ex);
//				_output.WriteLine(ex.ToString());
//				throw;
//			}
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
			return null;
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
						Query<BsonDocument>.EQ<string>(a => (string)a["_id"], artefact.Id),
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


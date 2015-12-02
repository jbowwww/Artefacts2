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
			ConfigureServiceStack();
		}
		
		/// <summary>
		/// Configures service stack - serialisers etc
		/// </summary>
		private static void ConfigureServiceStack()
		{
			JsConfig.TryToParsePrimitiveTypeValues = true;
			JsConfig.TreatEnumAsInteger = true;

			// ServiceStack setup
			//			JsConfig<Expression>.DeSerializeFn = s => new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer()).DeserializeText(s);
			JsConfig<Artefact>.SerializeFn = a => ServiceStack.StringExtensions.ToJsv<DataDictionary>(a./*Persisted*/Data);	// a.Data.ToJson();	// TypeSerializer.SerializeToString<DataDictionary>(a.Data);	// a.Data.SerializeToString();
			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { Data = ServiceStack.StringExtensions.FromJsv<DataDictionary>(a) }; // FromJsv<DataDictionary>() };	// TypeSerializer.DeserializeFromString<DataDictionary>(a) };//.FromJson<DataDictionary>() };
			JsConfig<BsonDocument>.SerializeFn = b => MongoDB.Bson.BsonExtensionMethods.ToJson(b);
			JsConfig<BsonDocument>.DeSerializeFn = b => BsonDocument.Parse(b);
//			JsConfig<QueryRequest>.SerializeFn = b => MongoDB.Bson.BsonExtensionMethods.ToJson<QueryRequest>(b);// b.ToJson();
//			JsConfig<QueryRequest>.DeSerializeFn = b => (QueryRequest)BsonDocument.Parse(b);
//			JsConfig<QueryDocument>.SerializeFn = q => q.ToJsv(); //((BsonDocument)q).AsByteArray;
			//			JsConfig<QueryDocument>.DeSerializeFn = q => q.FromJsv<QueryDocument>();
			//			JsConfig<IMongoQuery>.SerializeFn = q => q.ToJsv(); //((BsonDocument)q).AsByteArray;
			//			JsConfig<IMongoQuery>.DeSerializeFn = q => q.FromJsv<QueryDocument>();
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
//		private MongoCollection/*<Artefact>*/ _mcArtefacts;
		#endregion
		
		#region Properties
		public ExpressionVisitor Visitor {
			get { return _visitor ?? (_visitor = new ServerQueryVisitor()); }
		}
		private ExpressionVisitor _visitor = null;
		#endregion
		
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.Service.ArtefactsService"/> class.
		/// </summary>
		/// <param name="output">Output.</param>
		public ArtefactsService(TextWriter output)
		{
			_output = output;
			Log.DebugFormat("ArtefactsService({0})", output);
			Log.Info("Starting service");
//			ExpressionFormatter;
			// Storage (Mongo) setup
			_mClientSettings = new MongoClientSettings() { };		// TODO: Settings
			_mClient = new MongoClient("server=localhost");
			Log.Debug(_mClient);
			_output.WriteLine("mClient: " + _mClient);
			_mDb = new MongoDatabase(_mClient.GetServer(), "Artefacts", new MongoDatabaseSettings());
			Log.Debug(_mDb);
			
//			_mcArtefacts = _mDb.GetCollection/*<Artefact>*/("Artefacts");
//			Log.Debug(_mcArtefacts);
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
			Log.DebugFormat("Post({0})", artefact);
			_output.WriteLine("artefact: " + artefact);
			WriteConcernResult result = Save(artefact);
			Log.Debug(result);
			_output.WriteLine("result: " + result);
			return default(HttpWebResponse);
		}
		
		/// <summary>
		/// Put the specified artefact.
		/// </summary>
		/// <param name="artefact">Artefact.</param>
		public object Put(Artefact artefact)
		{
			Log.DebugFormat("Put({0})", artefact);
			_output.WriteLine("artefact: " + artefact);
			WriteConcernResult result = Save(artefact, SaveType.Update);	//InsertOrUpdate(artefact);
			Log.Debug(result);
			_output.WriteLine("result: " + result);
			return default(HttpWebResponse);// artefact;
		}
		
		/// <summary>
		/// Get the specified query.
		/// </summary>
		/// <param name="query">Query.</param>
		public QueryResults Get(QueryRequest query)
		{
			try {
				Log.DebugFormat("Get({0})", query);
				_output.WriteLine("Get({0})", query);
//				_output.WriteLine("{0}: {1}", query.Predicate.GetType(), ExpressionPrettyPrinter.PrettyPrint(query.Predicate));
//				Type elementType = query.Predicate.Parameters[0].Type;
//				Type queryType = typeof(Query<>).MakeGenericType(elementType);
//				MethodInfo whereMethod = queryType.GetMethod("Where");
//				LambdaExpression predicate = (LambdaExpression)Visitor.Visit(query.Predicate);
//				IMongoQuery mongoQuery = (IMongoQuery)whereMethod.Invoke(null, new object[] { predicate });
				MongoCollection<Artefact> _mcArtefacts = _mDb.GetCollection<Artefact>(query.CollectionName);	// elementType.Name);
				object result = _mcArtefacts.FindAs<Artefact>(query.Query);
				Log.Debug(result);
				List<Artefact> results = result != null ?
					((IEnumerable<Artefact>)result).ToList() :
					new List<Artefact>();
				QueryResults r = new QueryResults(results);
				Log.Debug(r);
				return r;
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				_output.WriteLine(ex.ToString());
				throw;
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
		
		/// <summary>
		/// Save the specified artefact and saveType.
		/// </summary>
		/// <param name="artefact">Artefact.</param>
		/// <param name="saveType">Save type.</param>
		private WriteConcernResult Save(Artefact artefact, SaveType saveType = SaveType.InsertOrUpdate)
		{
			try
			{
				MongoCollection<Artefact> _mcArtefacts = _mDb.GetCollection<Artefact>(artefact.Collection);
				BsonDocument artefactData = BsonDocument.Create(artefact.Data);
				WriteConcernResult result =
					saveType == SaveType.Insert ? _mcArtefacts.Insert(artefact)// _mcArtefacts.Insert<BsonDocument>(artefactData)
				:	saveType == SaveType.Update ? _mcArtefacts.Update(
						Query<Artefact>.EQ<string>(a => a.Id, artefact.Id),
						Update<Artefact>.Replace(artefact))//Update<BsonDocument>.Replace(artefactData))
				:	saveType == SaveType.InsertOrUpdate ? _mcArtefacts.Save(artefact)// _mcArtefacts.Save<BsonDocument>(artefactData)
				: default(WriteConcernResult);
				if (result.Ok)
					artefact.State = ArtefactState.Current;
				return result;
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				_output.WriteLine(ex.ToString());
				throw;
			}
		}
	}
}


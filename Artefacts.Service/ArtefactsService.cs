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

namespace Artefacts.Service
{
	public class ArtefactsService : ServiceStack.Service
	{
		private TextWriter _output = null;
		
		// TODO: Refactor out to a new storage class , keep it generic enough for possible storage provider changes?
		private MongoClient _mClient;
		private MongoClientSettings _mClientSettings;
		
		private MongoDatabase _mDb;
		private MongoCollection/*<Artefact>*/ _mcArtefacts;
		
		public ArtefactsService(TextWriter output)
		{
			// Debug
			_output = output;
			
			// ServiceStack setup
			
//			JsConfig<Expression>.DeSerializeFn = s => new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer()).DeserializeText(s);
		JsConfig<Artefact>.SerializeFn = a => StringExtensions.ToJsv<DataDictionary>(a.PersistedData);	// a.Data.ToJson();	// TypeSerializer.SerializeToString<DataDictionary>(a.Data);	// a.Data.SerializeToString();
			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { Data = a.FromJsv<DataDictionary>() };	// TypeSerializer.DeserializeFromString<DataDictionary>(a) };//.FromJson<DataDictionary>() };
//			JsConfig<QueryDocument>.SerializeFn = q => q.ToJsv(); //((BsonDocument)q).AsByteArray;
//			JsConfig<QueryDocument>.DeSerializeFn = q => q.FromJsv<QueryDocument>();
//			JsConfig<IMongoQuery>.SerializeFn = q => q.ToJsv(); //((BsonDocument)q).AsByteArray;
//			JsConfig<IMongoQuery>.DeSerializeFn = q => q.FromJsv<QueryDocument>();
			
			// Storage (Mongo) setup
			_mClientSettings = new MongoClientSettings() { };		// TODO: Settings
			_mClient = new MongoClient("server=localhost");
			_output.WriteLine("mClient: " + _mClient.ToString());//.FormatString(3, "  "));//.Dump());
			_mDb = new MongoDatabase(_mClient.GetServer(), "Artefacts", new MongoDatabaseSettings());
			
			_mcArtefacts = _mDb.GetCollection/*<Artefact>*/("Artefacts");
			MongoCollection c;
			
			
		}

		public object Put(Artefact artefact)
		{
			try
			{
				_output.WriteLine("Artefact artefact: " + artefact.ToString());
				artefact.State = ArtefactState.Current;
				// TODO: Look at inserting into Mongo using JSON received as service?
//				Request.GetRawBody(
//				BsonDocument.Parse();
				
				WriteConcernResult result = // _mcArtefacts.Insert(artefact);
//				_mcArtefacts.Insert<DataDictionary>(artefact.Data);
					_mcArtefacts.Insert<BsonDocument>(BsonDocument.Create(artefact.Data));
				return result.ToString();//.SerializeToString();
			}

			catch (Exception ex)
			{
				_output.WriteLine(ex.ToString());
				return string.Empty;
			}
			
	//			return default(HttpWebResponse);e
		}
		
//		public Artefact Get(QueryDocument q)
//		{
//			object result = _mcArtefacts.Find(q);
//			return null;
//		}
		
		public Artefact Get(MatchArtefactRequest request)
		{
//			try {
				//				ExpressionFormatter.ToString(
				//				ExpressionParameterReplacer

				//				ExpressionSerializer.	// Serialize.Linq
				
				//				Expression match = request.Match;
				//				request.Match.GetElementType()
				//				_mcArtefacts.AsQueryable(
				//				new QueryDocument
//				request.Match.Compile();
				
//				_output.WriteLine(request.Match != null ? request.Match.ToString() : "(null)");
				_output.WriteLine("{0}: {1}", request.Match.GetType(), ExpressionPrettyPrinter.PrettyPrint(request.Match));
				
//			TranslatedQuery query = MongoQueryTranslator.Translate(_mcArtefacts.AsQueryable<Disk>().Where((Expression<Func<Disk, bool>>)request.Match));
//					(_mcArtefacts.Where(request.Match)));
//				_output.WriteLine("{0}: {1}", query.GetType(), query.ToString());
//				Artefact artefact
			object result = _mcArtefacts.FindAs<Artefact>(Query<Disk>.Where((Expression<Func<Disk, bool>>)request.Match));
			List<object> results = new List<object>(); //result
//			object result = new MongoQueryProvider(_mcArtefacts).Execute<Disk>(request.Match);
//			object arr = ((IEnumerable<Disk>)result).ToArray();
			foreach (object r in (IEnumerable)result)
			{
				results.Add(r);
				_output.WriteLine(r.ToString());
			}
			
			return results.Count > 0 ? (Artefact)results[0] : null;
//			return ((IEnumerable)result).Cast<Artefact>().ToArray<Artefact>()[0];
			
//				return null;
//				query.Execute();
//				IQueryable<Artefact> allArtefacts = _mcArtefacts.FindAll().AsQueryable();
//				Artefact artefact = allArtefacts.Provider.Execute<Artefact>(request.Match);
//			MongoCursor<Artefact> cursor = (MongoCursor<Artefact>)result;
//			IQueryable<Artefact> cursor = (IQueryable<Artefact>)result;
//			Artefact artefact = cursor.First();
//			return artefact;
//			return (Artefact)((IQueryable<Artefact>)result).First();//artefact;
//			}
//			catch(Exception ex)
//			{
//				return null;
//			}
		}
		
		public QueryResults Get(QueryRequest query)
		{
			_output.WriteLine("{0}: {1}", query.Where.GetType(), ExpressionPrettyPrinter.PrettyPrint(query.Where));
			Type elementType = query.Where.Parameters[0].Type;
			Type filterFunc = typeof(Func<>).MakeGenericType(elementType);
			Type queryType = typeof(Query<>).MakeGenericType(elementType);
			MethodInfo whereMethod = queryType.GetMethod("Where");
			IMongoQuery mongoQuery = (IMongoQuery)whereMethod.Invoke(null, new object[] { query.Where });
			object result = _mcArtefacts.FindAs<Artefact>(mongoQuery);
			List<Artefact> results = ((IEnumerable<Artefact>)result).ToList();	// new List<object>(); //result
//			return null;
			return new QueryResults(results);
		}
		
		public object Any(object request)
		{
//			try
//			{
				_output.WriteLine("Any ! request: " + request.ToString());
				return default(HttpWebResponse);
//			}
//
//			catch (Exception ex)
//			{
//				_output.WriteLine(ex.ToString());
//			}
			return null;
		}
	}
}


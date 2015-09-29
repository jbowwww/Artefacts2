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
using System.Linq;
using System.Collections.Generic;

namespace Artefacts.Service
{
	public class ArtefactsService : ServiceStack.Service
	{
		private TextWriter _output = null;
		
		// TODO: Refactor out to a new storage class , keep it generic enough for possible storage provider changes?
		private MongoClient _mClient;
		private MongoClientSettings _mClientSettings;
		
		private MongoDatabase _mDb;
		private MongoCollection<Artefact> _mcArtefacts;
		
		public ArtefactsService(TextWriter output)
		{
			// Debug
			_output = output;
			
			// ServiceStack setup
//			JsConfig<Expression>.DeSerializeFn = s => new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer()).DeserializeText(s);
			JsConfig<Artefact>.SerializeFn = a => a.Data.SerializeToString();
			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { Data = a.FromJson<DataDictionary>() };

			// Storage (Mongo) setup
			_mClientSettings = new MongoClientSettings() { };
			
			// TODO: Settings
			_mClient = new MongoClient("server=localhost");
//			_output.Write(_mClient.ToString());//.Dump());
//			_mClient.Settings.ConvertTo<>();
			_mDb = new MongoDatabase(_mClient.GetServer(), "Artefacts", new MongoDatabaseSettings());
			
			_mcArtefacts = _mDb.GetCollection<Artefact>("Artefacts");
			
			
			
		}
		
		
//		public object Put(DataDictionary data)
//		{
//			try
//			{
//				_output.WriteLine("DataDictionary data: " + data.Dump());
//				return null;
//			}
//
//			catch (Exception ex)
//			{
//				_output.WriteLine(ex.ToString());
//			}
//			return null;
//			//			return default(HttpWebResponse);e
//		}

		public object Put(Artefact artefact)
		{
			try
			{
				_output.WriteLine("Artefact artefact: " + artefact.ToString());
				artefact.State = ArtefactState.Current;
				WriteConcernResult result = _mcArtefacts.Insert(
					artefact);
				return result.ToString();//.SerializeToString();
			}

			catch (Exception ex)
			{
				_output.WriteLine(ex.ToString());
				return string.Empty;
			}
			
	//			return default(HttpWebResponse);e
		}
		
		public Artefact Get(MatchArtefactRequest request)
		{
			try {
				_output.WriteLine(request.Match != null ? request.Match.ToString() : "(null)");
				IQueryable<Artefact> allArtefacts = _mcArtefacts.FindAll().AsQueryable();
				Artefact artefact = allArtefacts.Provider.Execute<Artefact>(request.Match);
//					new ParameterExpression[] {
				//					Expression.Parameter(typeof(IEnumerable<Artefact>))}).Compile());
				return artefact;
			}
			catch(Exception ex)
			{
				return null;
			}
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


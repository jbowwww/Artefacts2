using NUnit.Framework;
using System;
using ServiceStack;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using Artefacts.FileSystem;
using Artefacts.Service;
<<<<<<< HEAD
using ServiceStack.Text;
using MongoDB.Bson;
=======
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.

namespace Artefacts.TestClient
{
	[TestFixture]
	public class ArtefactsTestClient
	{
		private TextBufferWriter _bw;

		private string _serviceBaseUrl;

		private IServiceClient _client;

		private dynamic _artefact;

		//		[TestFixtureSetUp]
		public ArtefactsTestClient(string serviceBaseUrl, TextBufferWriter bufferWriter)
		{
			//			_client = client;
			_bw = bufferWriter;
			_serviceBaseUrl = serviceBaseUrl;
			_bw.Write(string.Format("Creating client to access {0} ... ", _serviceBaseUrl));
			_client = new ServiceStack.JsonServiceClient(_serviceBaseUrl) {
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
			bufferWriter.Write("OK\n");
			bufferWriter.Write("Creating test Artefact ... ");
			_artefact = new Artefact(new {
				Name = "Test",
				Desc = "Description",
				testInt = 18,
				testBool = false
			});//, testObjArray = new object[] { false, 2, "three", null } });
<<<<<<< HEAD
=======
			bufferWriter.Write(ServiceStack.StringExtensions.ToJson(_artefact) + "\n");
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
		}

		private void DoClientPut(object argument, string name = "[Unknown]")
		{
			try
			{
				// Don't need to receive and output response because _client already has request/response filters for that
				_bw.Write("DoClientPut: {0}: {1}\n", name, argument.GetType().FullName);
				_client.Put(argument);
			}
			catch (ServiceStack.WebServiceException wsex)
			{ 
				_bw.Write(
					string.Format("\nError: {0}: {1}\nStatus: {2}: {3}\nResponse: {4}\n{5}: {6}\n",
				              wsex.ErrorCode, wsex.ErrorMessage, wsex.StatusCode, wsex.StatusDescription, wsex.ResponseBody,
				              !string.IsNullOrWhiteSpace(wsex.ServerStackTrace) ? "ServerStackTrace" : "StackTrace",
				              !string.IsNullOrWhiteSpace(wsex.ServerStackTrace) ? wsex.ServerStackTrace : wsex.StackTrace));
				for (Exception _wsex = wsex.InnerException; _wsex != null; _wsex = _wsex.InnerException)
					_bw.Write("\n" + _wsex.ToString());
				_bw.Write("\n");
			}
			catch (InvalidOperationException ioex)
			{
				_bw.Write("\n" + ioex.ToString() + ioex.TargetSite.ToString());
				for (Exception _wsex = ioex.InnerException; _wsex != null; _wsex = _wsex.InnerException)
					_bw.Write("\n" + _wsex.ToString());
				_bw.Write("\n");
			}
			catch (Exception ex)
			{
				_bw.Write("\n" + ex.ToString());	
				for (Exception _wsex = ex.InnerException; _wsex != null; _wsex = _wsex.InnerException)	
					_bw.Write("\n" + _wsex.ToString());
				_bw.Write("\n");
			}
			finally
			{
				_bw.Write("\n");
			}
		}

		[Test]
		public void PrintFormatSamples()
		{
			//			bufferWriter.WriteLine("\tDump: " + _artefact.Dump() + "\t\t_artefact.ToBsonDocument()");
			_bw.WriteLine("\tServiceStack.StringExtensions.ToJson(_artefact) : " + ServiceStack.StringExtensions.ToJson(_artefact));
			_bw.WriteLine("\tServiceStack.StringExtensions.ToJsv(_artefact) : " + ServiceStack.StringExtensions.ToJsv(_artefact));
			_bw.WriteLine("\tServiceStack.StringExtensions.ToXml(_artefact) : " + ServiceStack.StringExtensions.ToXml(_artefact));
			_bw.WriteLine("\tServiceStack.StringExtensions.ToCsv(_artefact) : " + ServiceStack.StringExtensions.ToCsv(_artefact));
			_bw.WriteLine("\t" + typeof(BsonDocument).FullName + " : " + _artefact.ToBsonDocument());
			_bw.WriteLine();
		}
		
		[Test]
		public void PutArtefact()
		{
			DoClientPut(_artefact, "_artefact");
		}

		[Test]
		public void PutArtefactData()
		{
			DoClientPut(_artefact.Data, "_artefact.Data");
		}
		
		[Test]
		public void PutArtefactAlternativeSerializations()
		{
			byte[] artefactData = MongoDB.Bson.BsonExtensionMethods.ToBson(_artefact);
			string artefactJson = MongoDB.Bson.BsonExtensionMethods.ToJson(_artefact);
			//		string artefactJson_SS = ServiceStack.StringExtensions.ToJson(_artefact);
			//		string artefactJsv = ServiceStack.StringExtensions.ToJsv(_artefact);
			//		string artefactCsv = ServiceStack.StringExtensions.ToCsv(_artefact);
			MongoDB.Bson.BsonDocument bsonDocument = MongoDB.Bson.BsonExtensionMethods.ToBsonDocument(_artefact);
			byte[] bsonDocData = MongoDB.Bson.BsonExtensionMethods.ToBson(bsonDocument);
			//string bsonDocJson = MongoDB.Bson.BsonExtensionMethods.ToJson(bsonDocument);
			//		string bsonDocJson_SS = ServiceStack.StringExtensions.ToJson(bsonDocument);
			List<object> subjects = new List<object>(new object[] {
				new object[] { _artefact,					"Artefact" },
				new object[] { artefactData,				"Mongo BSON" },
				new object[] { artefactJson,				"Mongo JSON" },
				//			new object[] { artefactJson_SS,			"SS JSON" },
				//			new object[] { artefactJsv,				"SS JSV" },
				//			new object[] { artefactCsv,				"SS CSV" },
				new object[] { bsonDocument,				"Mongo BsonDocument" },
				new object[] { bsonDocData,				"Mongo BsonDocument -> Mongo BSON" }
				//new object[] { bsonDocJson,				"Mongo BsonDocument -> Mongo JSON" },
				//			new object[] { bsonDocJson_SS,			"Mongo BsonDocument -> SS JSON" }
			});

			_bw.Write("Data:\n\t" + subjects.Select(
				o => ((string)((object[])o)[1]).PadRight(32) + (((object[])o)[0].GetType().IsArray ?
			                                                ((object[])o)[0].ToString().Replace("[]", string.Format("[{0}]", ((Array)((object[])o)[0]).Length))
			                                                :	((object[])o)[0])).Join("\n\t") + "\n");
			Thread.Sleep(50);

			foreach (object[] subject in subjects)
				DoClientPut(subject[0], (string)subject[1]);
		}

		/// <summary>
		/// TODO: Next step needed is expression visitor to remove local variable
		/// </summary>
		[Test]
		public void PutArtefact_Disk_New()
		{
			ArtefactsClient client = new ArtefactsClient(_serviceBaseUrl, _bw);
			foreach (Disk disk in Disk.Disks)
			{
				//TODO: SOme way of using simple standard syntax such as new DIsk() or above .Disks static property
				// where this new instance can be passed to a client proxy that translates it as necessary
				// (e.g. Artefact[Data][Operation], posts to server, which via response indicates whether it created
				// this new artefact, it already found one (how to specify a unique arbitrary key for any artefact type??)
				// and updated it (return some/all differing values??) or it exists but was identical (all properties??)
				
				// One possible way
				Disk newDisk = client.Sync<Disk>(d => (d.Name == disk.Name), () => disk);
<<<<<<< HEAD
				newDisk.GetId();
				_bw.WriteLine(newDisk.SerializeAndFormat());
=======
>>>>>>> parent of 77346bb... Updated client/server with JsConfig<Artefact>.Serializer (or something) to a custom serializer that srializes what it wants of the Artefact intsances (shuld be easy to experiment and customise this). Client proxy has Sync<T>() method which checks a predicate to see if equiv artefact already exists, if not creates one usign another predicate.
				
				// Another possible way If Disk implements IEquatable<T>
//				client.Sync<Disk>(disk);
				
				// SImilar to above but without generic parameter
//				client.Sync(disk);
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net;
using Artefacts;
using Artefacts.FileSystem;
using Artefacts.Service;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using ServiceStack;
using NUnit.Framework;
using ServiceStack.Text;
using MongoDB.Bson;

namespace Artefacts.TestClient
{
	/// <summary>
	/// Artefacts test client.
	/// </summary>
	/// <remarks>
	/// TODO: Move generic functionality to <see cref="TestClientBase"/> 
	/// </remarks>
	[TestFixture]
	public class ArtefactsTestClient : TestClientBase
	{
		#region Private fields
		private TextBufferWriter _bufferWriter;
		private string _serviceBaseUrl;
		private IServiceClient _client;
//		private ArtefactsClient _artefactsClient;
		private dynamic _artefact;
		private MainWindow _win;
		private string _winBaseTitle;
		#endregion
		
		//[TestFixtureSetUp]
		public ArtefactsTestClient(string serviceBaseUrl, TextBufferWriter bufferWriter, MainWindow win) : base(bufferWriter)
		{
			_bufferWriter = bufferWriter;
			_serviceBaseUrl = serviceBaseUrl;
			_win = win;
			_winBaseTitle = _win.Title;
			
			_bufferWriter.WriteLine(string.Format("Creating client to access {0} ... ", _serviceBaseUrl));
			_client = new JsonServiceClient(_serviceBaseUrl) {
				RequestFilter = (HttpWebRequest request) => bufferWriter.WriteLine(
					string.Format("Client.{0} HTTP {6} {5} {2} bytes {1} Expect {7} Accept {8}",
				              request.Method, request.ContentType,  request.ContentLength,
				              request.UserAgent, request.MediaType, request.RequestUri,
				              request.ProtocolVersion, request.Expect, request.Accept)),
				ResponseFilter = (HttpWebResponse response) => bufferWriter.WriteLine(
					string.Format(" --> {0} {1}: {2} {3} {5} bytes {4}",
				              response.StatusCode, response.StatusDescription, response.CharacterSet,
				              response.ContentEncoding, response.ContentType, response.ContentLength))
			};
//			JsConfig.TryToParsePrimitiveTypeValues = true;
//			JsConfig.TreatEnumAsInteger = true;
			JsConfig<Artefact>.SerializeFn = a => StringExtensions.ToJsv<DataDictionary>(a./*Persisted*/Data);	// a.Data.ToJson();	// TypeSerializer.SerializeToString<DataDictionary>(a.Data);	// a.Data.SerializeToString();
			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { Data = a.FromJsv<DataDictionary>() };	// TypeSerializer.DeserializeFromString<DataDictionary>(a) };//.FromJson<DataDictionary>() };
			JsConfig<BsonDocument>.SerializeFn = b => MongoDB.Bson.BsonExtensionMethods.ToJson(b);
			JsConfig<BsonDocument>.DeSerializeFn = b => BsonDocument.Parse(b);
//			JsConfig<QueryRequest>.SerializeFn = b => MongoDB.Bson.BsonExtensionMethods.ToJson<QueryRequest>(b);// b.ToJson();
//			JsConfig<QueryRequest>.DeSerializeFn = b => (QueryRequest)BsonDocument.Parse(b);
			bufferWriter.WriteLine(_client.ToString());
			bufferWriter.WriteLine("Creating test Artefact ... ");
			_artefact = new Artefact(new {
				Name = "Test",
				Desc = "Description",
				testInt = 18,
				testBool = false
			});
			bufferWriter.WriteLine("\tJSON: " + _artefact.ToString());//k.StringExtensions.ToJson(_artefact));
			bufferWriter.WriteLine();
			
//			_artefactsClient = new ArtefactsClient(_serviceBaseUrl, _bufferWriter);
//			_bufferWriter.WriteLine("_artefactsClient: {0}", _artefactsClient);
		}

//		[Test]
		public void GetDisk()
		{
			QueryResults testResult = _client.Get<QueryResults>((QueryRequest)QueryRequest.Make<Disk>(d => (d.Name == "sda")));
			_bufferWriter.WriteLine("testResult = " + testResult);
		}

//		[Test]
		public void GetOrCreateDisk()
		{
			foreach (Disk disk in Disk.Disks)
			{
				Disk testDisk;
				QueryResults results = _client.Get<QueryResults>(QueryRequest.Make<Disk>(d => d.Name == disk.Name)); 	//_artefactsClient.GetOrCreate<Disk>(d => (d.Name == disk.Name), () => disk);
				_bufferWriter.WriteLine("results = " + results);	//.FormatString());
				if (results.Artefacts.Count() > 0)
				{
					testDisk = results.Artefacts.ElementAt(0).As<Disk>();
					_bufferWriter.WriteLine("testDisk = " + testDisk);
				}
				else
				{
					Artefact newArtefact = new Artefact(disk);
					_bufferWriter.WriteLine("newArtefact = " + newArtefact);	//.FormatString());
					_client.Post(newArtefact);
				}
			}
		}

//		[Test]
		public void GetDrive()
		{
			QueryResults testResult = _client.Get<QueryResults>(QueryRequest.Make<Drive>(d => (d.Name == "sda")));
			_bufferWriter.WriteLine("testResult = " + testResult);
		}

//		[Test]
		public void GetOrCreateDrive()
		{
			foreach (Drive drive in Drive.All)
			{
				Drive testDrive;
				QueryResults results = _client.Get<QueryResults>(QueryRequest.Make<Drive>(d => d.Name == drive.Name)); 	//_artefactsClient.GetOrCreate<Disk>(d => (d.Name == disk.Name), () => disk);
				_bufferWriter.WriteLine("results = " + results);	//.FormatString());
				if (results.Artefacts.Count() > 0)
				{
					testDrive = results.Artefacts.ElementAt(0).As<Drive>();
					_bufferWriter.WriteLine("testDisk = " + testDrive);
				}
				else
				{
					Artefact newArtefact = new Artefact(drive);
					_bufferWriter.WriteLine("newArtefact = " + newArtefact);	//.FormatString());
					_client.Post(newArtefact);
				}
			}
		}

//		[Test]
		public void RecurseDirectory()
		{
			Thread[] threads = new Thread[4];
			int mainCount = 0;
			TimeSpan totalTime = TimeSpan.Zero;
			DateTime mainStart = DateTime.Now;
			_bufferWriter.WriteLine("RecurseDirectory() starting at " + mainStart.ToShortTimeString());
//			_directoryQueue.Clear();
			Directory dir = new Directory("/mnt/Trapdoor/mystuff/Moozik/");
			_bufferWriter.WriteLine("Main thread starting directory " + dir.Path);
			RecurseDirectory(dir);
			mainCount++;

			for (int numThreads = 0; numThreads < 4; numThreads++)
			{
				threads[numThreads] = new Thread((param) => {
					int num = (int)param;
					int count = 0;
					DateTime threadStart = DateTime.Now;
					_bufferWriter.WriteLine("Thread #" + num + " starting at " + threadStart.ToShortTimeString());
					
					while (_directoryQueue.Count > 0)
					{
						try
						{
							Directory threadDir;
							if (!_directoryQueue.TryDequeue(out threadDir))
								throw new InvalidOperationException("Could not dequeue directory");
							_bufferWriter.WriteLine("Thread #" + num + " running on directory number " + count + " at " + DateTime.Now.ToShortTimeString() + " " + threadDir.Path);
							RecurseDirectory(threadDir);
							count++;
						}
						catch (Exception ex)
						{
							_bufferWriter.WriteLine(ex.Format());
							continue;
						}
					}
					DateTime threadFinish = DateTime.Now;
					TimeSpan threadDuration = threadFinish - threadStart;
					_bufferWriter.WriteLine("Thread #" + num + " finished at " + threadFinish.ToShortTimeString() + " : Total time " + threadDuration + ", totalled " + count + " directories");
					mainCount += count;
					totalTime += threadDuration;
				});
				threads[numThreads].Start(numThreads);
			}
			
			DateTime mainFinish = DateTime.Now;
			TimeSpan mainDuration = mainFinish - mainStart;
			totalTime += mainDuration;
			_bufferWriter.WriteLine("Main thread finished at " + mainFinish.ToShortTimeString() + " : Time " + mainDuration);
			for (int i = 0; i < 4; i++)
			{
				
				while (!threads[0].Join(1000));
				_win.Title = _winBaseTitle + " " + (DateTime.Now - mainStart);
			}
			_bufferWriter.WriteLine("All threads finished at " + DateTime.Now + ", total time " + totalTime + ", " + mainCount + " directories traversed");
		}
		private ConcurrentQueue<Directory> _directoryQueue = new ConcurrentQueue<Directory>();
		private ConcurrentBag<FileSystemEntry> _failedEntries = new ConcurrentBag<FileSystemEntry>();
		
		public void RecurseDirectory(Directory dir, int maxDepth = 2, int currentLevel = 0)
		{
			int retries;
			if (currentLevel < maxDepth)
			{
				for (retries = 0; retries < 3; retries++)
				{
					try
					{
//				_artefactsClient.GetOrCreate(d => d.Path != null && dir.Path != null && d.Path == dir.Path, () => dir);
//						_artefactsClient.Save<Directory>(d => d.Path != null && d.Path == dir.Path, dir);	// () => dir);
						if (_client.Get<QueryResults>(QueryRequest.Make<Directory>(d => d.Path == dir.Path)).Artefacts.Count() == 0)
							_client.Post(new Artefact(dir));
						break;
					}
					catch (WebException ex)
					{
						_bufferWriter.WriteLine(ex.Format());
						continue;
					}
				}
				if (retries == 3)
				{	
					_bufferWriter.WriteLine("! ERROR! failed after 3 retries on directory " + dir.Path);
					_failedEntries.Add(dir);
				}
				
				foreach (File file in dir.Files)
				{
//					_artefactsClient.GetOrCreate<File>(f => f.Path != null && file.Path != null && f.Path == file.Path, () => file);
					for (retries = 0; retries < 3; retries++)
					{
						try
						{
//							_artefactsClient.Save<File>(f => f.Path != null && f.Path == file.Path, file);	//() => file);
							if (_client.Get<QueryResults>(QueryRequest.Make<File>(f => f.Path == file.Path)).Artefacts.Count() == 0)
								_client.Post(new Artefact(file));
							break;
						}
						catch (WebException ex)
						{
							_bufferWriter.WriteLine(ex.Format());
							continue;
						};
					}
					if (retries == 3)
					{	
						_bufferWriter.WriteLine("! ERROR! failed after 3 retries on file " + file.Path);
						_failedEntries.Add(file);
					}
				}
				
				foreach (Directory sd in dir.Directories)
				{
					_directoryQueue.Enqueue(sd);
//					RecurseDirectory(sd, currentLevel + 1);
				}
			}
		}
		
//		[Test]
		public void GetFiles()
		{
			File testFile;
			QueryResults results = _client.Get<QueryResults>(QueryRequest.Make<File>(f => f.Path != null));
			_bufferWriter.WriteLine("results = " + results);
			foreach (Artefact artefact in results.Artefacts)
				_bufferWriter.WriteLine("results.Artefacts[] = " + artefact);
			_bufferWriter.WriteLine("Total results: " + results.Count);
		}
		
//		[Test]
		public void GetLargeFiles()
		{
			//QueryResults testResult = _artefactsClient.Get<File>(f => f.Size > (Int64) (100 * 1024 * 1024));
			QueryRequest request = QueryRequest.Make<File>(f => (f.Size  > (100 * 1024  )));
			QueryResults results = _client.Get<QueryResults>(request);
			_bufferWriter.WriteLine("results = " + results);
			foreach (Artefact artefact in results.Artefacts)
				_bufferWriter.WriteLine("results.Artefacts[] = " + artefact);
		}
		
		[Test]
		public void GetLargeFilesWithDupeSize()
		{
			//QueryResults testResult = _artefactsClient.Get<File>(f => f.Size > (Int64) (100 * 1024 * 1024));
			QueryRequest request = QueryRequest.Make<File>(f => (f.Size  > (16 * 1024 * 1024)));
			QueryResults results = _client.Get<QueryResults>(request);
			_bufferWriter.WriteLine(string.Format("{0} file results >= 16MB ", results.Count));
			long i = 0;
			long totalSize = 0;
			foreach (Artefact artefact in results.Artefacts)
			{
				long groupSize = 0;
				File file = artefact.As<File>();
				QueryRequest request2 = QueryRequest.Make<File>(f => f.Size == file.Size);
				QueryResults results2 = _client.Get<QueryResults>(request2);
				_bufferWriter.WriteLine(string.Format("result #{0}: f.Size = {1} ({2} file results with matching size)",
					i++, File.FormatSize(file.Size), results2.Count));
				foreach (Artefact artefact2 in results2.Artefacts)
				{
					File file2 = artefact2.As<File>();
					_bufferWriter.WriteLine("\t" + file2.Path);
					groupSize += file2.Size;
				}
				_bufferWriter.WriteLine("Total " + File.FormatSize(groupSize) + " bytes in group\n");	// results2.Artefacts.Sum(a => a.As<File>().Size)
				totalSize += groupSize;
			}
			_bufferWriter.WriteLine("Total " + File.FormatSize(totalSize) + " bytes in all groups");
		}
		
		#region Helper functions
		/// <summary>
		/// 
		/// </summary>		
		private void DoClientPut(object argument, string name = "[Unknown]")
		{
			try
			{
				// Don't need to receive and output response because _client already has request/response filters for that
				_bufferWriter.Write("DoClientPut: {0}: {1}\n", name, argument.GetType().FullName);
				_client.Put(argument);
			}
			catch (ServiceStack.WebServiceException wsex)
			{ 
				_bufferWriter.Write(
					string.Format("\nError: {0}: {1}\nStatus: {2}: {3}\nResponse: {4}\n{5}: {6}\n",
				              wsex.ErrorCode, wsex.ErrorMessage, wsex.StatusCode, wsex.StatusDescription, wsex.ResponseBody,
				              !string.IsNullOrWhiteSpace(wsex.ServerStackTrace) ? "ServerStackTrace" : "StackTrace",
				              !string.IsNullOrWhiteSpace(wsex.ServerStackTrace) ? wsex.ServerStackTrace : wsex.StackTrace));
				for (Exception _wsex = wsex.InnerException; _wsex != null; _wsex = _wsex.InnerException)
					_bufferWriter.Write("\n" + _wsex.ToString());
				_bufferWriter.Write("\n");
			}
			catch (InvalidOperationException ioex)
			{
				_bufferWriter.Write("\n" + ioex.ToString() + ioex.TargetSite.ToString());
				for (Exception _wsex = ioex.InnerException; _wsex != null; _wsex = _wsex.InnerException)
					_bufferWriter.Write("\n" + _wsex.ToString());
				_bufferWriter.Write("\n");
			}
			catch (Exception ex)
			{
				_bufferWriter.Write("\n" + ex.ToString());	
				for (Exception _wsex = ex.InnerException; _wsex != null; _wsex = _wsex.InnerException)	
					_bufferWriter.Write("\n" + _wsex.ToString());
				_bufferWriter.Write("\n");
			}
			finally
			{
				_bufferWriter.Write("\n");
			}
		}
		#endregion
		
		#region Old inactive and/or failed/abandoned tests
		//		[Test]
//		public void GetOrCreateDriveOld()
//		{
//			foreach (Drive disk in Drive.All)
//			{
//				Drive testDrive = _artefactsClient.GetOrCreate<Drive>(d => (d.Name == disk.Name), () => disk);
//				_bufferWriter.WriteLine("testResult = " + testDrive.FormatString());
//			}
//		}
		//		[Test]
		public void PutArtefact()
		{
			DoClientPut(_artefact, "_artefact");
		}

		//		[Test]
		public void PutArtefactData()
		{
			DoClientPut(_artefact.Data, "_artefact.Data");
		}

		//		[Test]
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

			_bufferWriter.Write("Data:\n\t" + subjects.Select(
				o => ((string)((object[])o)[1]).PadRight(32) + (((object[])o)[0].GetType().IsArray ?
			                                                ((object[])o)[0].ToString().Replace("[]", string.Format("[{0}]", ((Array)((object[])o)[0]).Length))
			                                                :	((object[])o)[0])).Join("\n\t") + "\n");
			Thread.Sleep(50);

			foreach (object[] subject in subjects)
				DoClientPut(subject[0], (string)subject[1]);
		}

		//		[Test]
		//		public void SyncDiskAsArtefact()
		//		{
		//			foreach (Disk disk in Disk.Disks)
		//			{
		//				// One possible way
		//				//				Artefact newDisk = client.Sync<Artefact>(d => (d.Name == disk.Name), () => new Artefact(disk));
		//				Artefact newDisk = _artefactsClient.Sync<Artefact>((dynamic a) => (a.Name == disk.Name), () => new Artefact(disk));
		//				_bufferWriter.WriteLine("newDisk = " + newDisk.FormatString());
		//			}
		//		}

		//		[Test]
		//		public void GetDiskAsArtefact()
		//		{
		//			QueryResults testResult = _client.Get<QueryResults>(QueryRequest.Make(a => (a.Name == "sda")));
		//			_bufferWriter.WriteLine("testResult = " + testResult.FormatString());	//.ToJsv());
		//
		//		}	

		/// <summary>
		/// "Save" Disk.disks instances - i.e. if they already exist, update them, otherwise, create
		/// </summary>
		//		[Test]
//		public void SaveDisk()
//		{
//			foreach (Disk disk in Disk.Disks)
//			{
//				bool isNewDisk = _artefactsClient.Save<Disk>(d => (d.Name == disk.Name), disk);
//				_bufferWriter.WriteLine("isNewDisk = " + isNewDisk.FormatString());
//			}
//		}	
		#endregion
	}
}

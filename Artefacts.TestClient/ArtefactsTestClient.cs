using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Artefacts;
using Artefacts.Extensions;
using Artefacts.FileSystem;
using Artefacts.Service;
using Artefacts.Service.Extensions;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using ServiceStack;
using NUnit.Framework;
using ServiceStack.Text;
using MongoDB.Bson;
using System.Dynamic.Utils;

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
//				RequestFilter = (HttpWebRequest request) => bufferWriter.WriteLine(
//					string.Format("Client.{0} HTTP {6} {5} {2} bytes {1} Expect {7} Accept {8}",
//				              request.Method, request.ContentType,  request.ContentLength,
//				              request.UserAgent, request.MediaType, request.RequestUri,
//				              request.ProtocolVersion, request.Expect, request.Accept)),
//				ResponseFilter = (HttpWebResponse response) => bufferWriter.WriteLine(
//					string.Format(" --> {0} {1}: {2} {3} {5} bytes {4}",
//				              response.StatusCode, response.StatusDescription, response.CharacterSet,
//				              response.ContentEncoding, response.ContentType, response.ContentLength))
			};
//			JsConfig.TryToParsePrimitiveTypeValues = true;
			JsConfig.TreatEnumAsInteger = true;
			JsConfig<Artefact>.IncludeTypeInfo = true;
			JsConfig.TryToParseNumericType = false;
			
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
			int maxDepth = -1;	// max FS file depth
			int mainCount = 0;
			DateTime mainStart = DateTime.Now;
			TimeSpan totalTime = TimeSpan.Zero;
			int numThreads = 4;
			Thread[] threads = new Thread[numThreads];
			int postThreadSleepTime = 224;
			int scanThreadSleepTime = 88;
			int scanThreadActiveCount = 0;
			int maxPostRetries = 3;
			ConcurrentQueue<FileSystemEntry> _postQueue = new ConcurrentQueue<FileSystemEntry>();
			ConcurrentQueue<Directory> _directoryQueue = new ConcurrentQueue<Directory>();
			
			Directory topDir = new Directory("/mnt/Trapdoor/mystuff/");
			_directoryQueue.Enqueue(topDir);
			
			_bufferWriter.WriteLine("RecurseDirectory() starting at " + mainStart.ToShortTimeString());
			_bufferWriter.WriteLine("Main thread starting directory " + topDir.Path);

			Thread _postThread = new Thread(() =>
			{
				int count = 0;
				FileSystemEntry fsEntry;
				DateTime threadStart = DateTime.Now;
				_bufferWriter.WriteLine("POST Thread starting at " + threadStart.ToShortTimeString());
				Thread.Sleep(postThreadSleepTime);
				while (_postQueue.Count > 0 || Volatile.Read(ref scanThreadActiveCount) > 0)
				{
					if (_postQueue.Count == 0)
						Thread.Sleep(postThreadSleepTime);
					else
					{
						while (_postQueue.TryDequeue(out fsEntry))
						{
							for (int i = 0; i < maxPostRetries; i++)
							{
								try 
								{
									File file = fsEntry as File;
									Directory dir = fsEntry as Directory;
									if (file != null)
									{
										_bufferWriter.WriteLine("Creating (or updating) File \"{0}\"", file.Path);
										_client.GetOrCreate<File>((f) => f.Path == file.Path, () => file);
									}
									else if (dir != null)
									{
										_bufferWriter.WriteLine("Creating (or updating) Directory \"{0}\"", dir.Path);
										_client.GetOrCreate<Directory>((d) => d.Path == dir.Path, () => dir);
									}
									count++;
									break;
								}
								catch (System.Net.WebException ex)
								{
									Log.ErrorFormat(" ! ERROR: {0}: Retry attempt #{1} of {2}\n", ex.GetType().FullName, i + 1, maxPostRetries);
									continue;
								}
							}
						}
					}
				}
				DateTime threadFinish = DateTime.Now;
				TimeSpan threadDuration = threadFinish - threadStart;
				_bufferWriter.WriteLine("POST Thread finished at " + threadFinish.ToShortTimeString() + " : Total time " + threadDuration + ", totalled " + count + " files");
				totalTime += threadDuration;
			});

			for (int tn = 0; tn < numThreads; tn++)
			{
				threads[tn] = new Thread((param) =>
				{
					int num = (int)param;
					int count = 0;
					DateTime threadStart = DateTime.Now;
					_bufferWriter.WriteLine("Thread #" + num + " starting at " + threadStart.ToShortTimeString());	
					try
					{
						Directory threadDir;
						while (_directoryQueue.Count > 0 || Volatile.Read(ref scanThreadActiveCount) > 0)
						{
							if (_directoryQueue.Count == 0)
								Thread.Sleep(scanThreadSleepTime);
							else
							{
								while (_directoryQueue.TryDequeue(out threadDir))
								{
									Volatile.Write(ref scanThreadActiveCount, Volatile.Read(ref scanThreadActiveCount) + 1);
									try
									{
										bool isExpired = DateTime.Now - threadDir.LastWriteTime > TimeSpan.FromMinutes(10);
										_bufferWriter.WriteLine("Thread #" + num + " running on directory number " + count + " at " +
											DateTime.Now.ToShortTimeString() + " " + threadDir.Path + ": " +
											(isExpired ? "Expired" : "Current"));
										if (isExpired)
										{
											foreach (Directory sd in threadDir.Directories)
											{
												int depth = sd.Path.Count((ch) => ch == '/' || ch == '\\');
												if (maxDepth < 0 || depth < maxDepth)
													_directoryQueue.Enqueue(sd);
												_postQueue.Enqueue(sd);
											}
											foreach (File file in threadDir.Files)
											{
												file.QueueCalculateCRC();
												_postQueue.Enqueue(file);
											}
										}
										count++;
									}
									catch (Exception ex)
									{
										_bufferWriter.WriteLine(ex.Format());
										continue;
									}
									finally
									{
										Volatile.Write(ref scanThreadActiveCount, Volatile.Read(ref scanThreadActiveCount) - 1);
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						_bufferWriter.WriteLine(ex.Format());
					}
					DateTime threadFinish = DateTime.Now;
					TimeSpan threadDuration = threadFinish - threadStart;
					_bufferWriter.WriteLine("Thread #" + num + " finished at " + threadFinish.ToShortTimeString() + " : Total time " + threadDuration + ", totalled " + count + " directories");
					mainCount += count;
					totalTime += threadDuration;
				});
				threads[tn].Start(tn);
			}
//			int t = 0;
//			threads.Each(thread => thread.Start(t++));
			_postThread.Start();
			
			DateTime mainFinish = DateTime.Now;
			TimeSpan mainDuration = mainFinish - mainStart;
			totalTime += mainDuration;
			_bufferWriter.WriteLine("Main thread finished at " + mainFinish.ToShortTimeString() + " : Time " + mainDuration);
			_postThread.Join();
			_bufferWriter.WriteLine("All threads finished at " + DateTime.Now + ", total time " + totalTime + ", " + mainCount + " directories traversed");
		}
		private ConcurrentBag<FileSystemEntry> _failedEntries = new ConcurrentBag<FileSystemEntry>();
		
//		[Test]
		public void GetFiles()
		{
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
			Dictionary<long, QueryResults> sizeGroups = new Dictionary<long, QueryResults>();	// file size keyed
			Dictionary<long, QueryResults> dupeGroups = new Dictionary<long, QueryResults>();	// file CRC keyed
			QueryRequest request = QueryRequest.Make<File>(f => (f.Size  > (16 * 1024 * 1024)));
			QueryResults results = _client.Get<QueryResults>(request);
			_bufferWriter.WriteLine(string.Format("{0} file results >= 16MB ", results.Count));
			long i = 0;
			long totalSize = 0;
			foreach (Artefact artefact in results.Artefacts)
			{
				long groupSize = 0;
				File file = artefact.As<File>();
				if (!sizeGroups.ContainsKey(file.Size))
				{
					QueryResults results2 = _client.Get<QueryResults>(QueryRequest.Make<File>(f => (f.Size == file.Size)));
					sizeGroups.Add(file.Size, results2);
					if (results2.Count > 1)
					{
						_bufferWriter.WriteLine(string.Format(
							"result #{0}: f.Size = {1} ({2} file results with matching size)",
							i++, File.FormatSize(file.Size), results2.Count));
						IEnumerable<File> files = results2.Artefacts.Select(a => a.As<File>());
						files.Each(f => { f.QueueCalculateCRC(); });
						foreach (File file2 in files)
						{
							file2.CRCWaitHandle.WaitOne();
							long crc = file2.CRC.Value;
							if (!dupeGroups.ContainsKey(crc))
								dupeGroups.Add(crc, new QueryResults(new Artefact[] { Artefact.Cache.GetArtefact(file2) }));	//new Artefact[] { artefact2 }));
							else
								dupeGroups[crc].Add(Artefact.Cache.GetArtefact(file2));
							_client.Put(Artefact.Cache.GetArtefact(file2));
							_bufferWriter.WriteLine("    " + file2.CRC.Value.ToHex().PadLeft(26) + file2.Path);
							groupSize += file2.Size;
						}
						_bufferWriter.WriteLine("Total " + File.FormatSize(groupSize) + " in group\n");	// results2.Artefacts.Sum(a => a.As<File>().Size)
						totalSize += groupSize;
						_bufferWriter.WriteLine("Total " + File.FormatSize(totalSize) + " in all groups\n");
					}
					
				}
			}
			totalSize = 0;
			foreach (KeyValuePair<long, QueryResults> qrPair in dupeGroups)
			{
				QueryResults qr = qrPair.Value;
				if (qr.Count > 1)
				{
					long groupSize = 0;
					File file = qr.Artefacts[0].As<File>();
					_bufferWriter.WriteLine(string.Format(
						"result #{0}: f.Size = {1} f.CRC = {2} ({3} file results with matching CRC)",
						i++, File.FormatSize(file.Size), qrPair.Key.ToHex() /*file.CRC*/, qr.Count));
					foreach (Artefact artefact2 in qr.Artefacts)
					{
						file = artefact2.As<File>();
						_bufferWriter.WriteLine("    " + file.Path);
						groupSize += file.Size;
					}
					_bufferWriter.WriteLine("Total " + File.FormatSize(groupSize) + " in group");
					totalSize += groupSize;
					new DupeProcessWindow(qr.Artefacts.Select<Artefact,File>(a => a.As<File>())).Show();
				}
			}
			int totalUsedGroups = dupeGroups.Count(pair => pair.Value.Count > 1);
			_bufferWriter.WriteLine("Total " + File.FormatSize(totalSize) + " in " + totalUsedGroups + "/" + dupeGroups.Count + " groups");
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

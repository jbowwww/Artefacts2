using System;
using System.Collections.Generic;
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
//using ServiceStack.Text;
using MongoDB.Bson;
using System.Dynamic.Utils;
using MongoDB.Bson.Serialization;
using System.Reflection;
using MongoDB.Driver.Linq;

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
//		public class TestClientSettings {
//			private ArtefactsTestClient _testClient;
//			
//			public TimeSpan? Timeout {
//				get { return _testClient._client.Timeout; }
//				set { _testClient._client.Timeout = value; }
//			}
//
//			public Action<HttpWebRequest> RequestFilter {
//				get { return _testClient._client.RequestFilter; }
//				set { _testClient._client.RequestFilter = value; }
//			}
//
//			public Action<HttpWebResponse> ResponseFilter {
//				get { return _testClient._client.ResponseFilter; }
//				set { _testClient._client.ResponseFilter = value; }
//			}
//
//			internal TestClientSettings() {}
//			
//			public TestClientSettings(ArtefactsTestClient testClient)
//			{
//				if (testClient == null)
//					throw new ArgumentNullException("testClient");
//				_testClient = testClient;
//			}
//			
//			internal void SetTestClient(ArtefactsTestClient testClient)
//			{
//				_testClient = testClient;
//			}
//		}
//	
//		public static readonly TestClientSettings DefaultSettings = new TestClientSettings() {
//			Timeout = TimeSpan.FromSeconds(15),
//			RequestFilter = null,
//			ResponseFilter = null
//		};

		internal void OptionalRequestFilter(HttpWebRequest request)
		{
			_bufferWriter.WriteLine(string.Format("Client.{0} HTTP {6} {5} {2} bytes {1} Expect {7} Accept {8}",
				request.Method, request.ContentType, request.ContentLength, request.UserAgent, request.MediaType,
				request.RequestUri, request.ProtocolVersion, request.Expect, request.Accept));
		}
		
		internal void OptionalResponseFilter(HttpWebResponse response)
		{
			_bufferWriter.WriteLine(
				string.Format(" --> {0} {1}: {2} {3} {5} bytes {4}",
					response.StatusCode, response.StatusDescription, response.CharacterSet,
					response.ContentEncoding, response.ContentType, response.ContentLength));
		}
		
		#region Private fields
		private TextBufferWriter _bufferWriter;
		private string _serviceBaseUrl;
//		private IServiceClient _client;
		private ServiceClientBase _client;
//		private ArtefactsClient _artefactsClient;
		private dynamic _artefact;
		private MainWindow _win;
//		private string _winBaseTitle;
		#endregion
		
//		public readonly TestClientSettings Settings = DefaultSettings;
		
		public ConcurrentQueue<FileSystemEntry> DelQueue = new ConcurrentQueue<FileSystemEntry>();
		public ConcurrentQueue<FileSystemEntry> PostQueue = new ConcurrentQueue<FileSystemEntry>();
		public ConcurrentDictionary<Directory, bool> DirectoryList = new ConcurrentDictionary<Directory, bool>();
		
		ArtefactCollection<Directory> dirCollection;
		ArtefactCollection<File> fileCollection;
		
		//[TestFixtureSetUp]
		public ArtefactsTestClient(string serviceBaseUrl, TextBufferWriter bufferWriter, MainWindow win) : base(bufferWriter)
		{
			GLib.ExceptionManager.UnhandledException += (args) => { Log.Error(args); };
			
			_bufferWriter = bufferWriter;
			_serviceBaseUrl = serviceBaseUrl;
			_win = win;
//			_winBaseTitle = _win.Title;
			
			_bufferWriter.WriteLine(string.Format("Creating client to access {0} ... ", _serviceBaseUrl));
			_client = new JsonServiceClient(_serviceBaseUrl) {
				Timeout = TimeSpan.FromSeconds(15)
			};

			Artefact.ConfigureServiceStack();

			win.GetPostQueueCount += () => PostQueue.Count;
			win.GetDirQueueCount += () => DirectoryList.Count;
			win.GetCRCQueueCount += () => Artefacts.FileSystem.FileCRC.QueueCount;
			win.GetCurrentTest += () => CurrentTest;// == null ? "" : CurrentTest.Name;
			win.GetCurrentTestTime += () => CurrentTestStartTime;
			
			bufferWriter.WriteLine(_client.ToString());
			bufferWriter.WriteLine("Creating test Artefact ... ");
			_artefact = new Artefact(new {
				Name = "Test",
				Desc = "Description",
				testInt = 18,
				testBool = false
			});
			bufferWriter.WriteLine("\tJSON: " + _artefact.ToString());//k.StringExtensions.ToJson(_artefact));
			
			dirCollection = new ArtefactCollection<Directory>(_client);
			fileCollection = new ArtefactCollection<File>(_client);	//, "Artefacts_FileSystem_File");

		}
		
		public override void Dispose()
		{
			_client.Dispose();
		}
		
		public override bool OnStartingTests()
		{
			_win.EnableStatusGetters = true;
			_win.EnableStartButton = false;
			return base.OnStartingTests();
		}
		
		public override void OnFinishedTests(IEnumerable<MethodInfo> tests, IEnumerable<DateTime> startTimes, IEnumerable<DateTime> finishTimes, IEnumerable<TimeSpan> durations, TimeSpan totalDuration, int totalFailed, IEnumerable<Exception> exceptions)
		{
			_bufferWriter.WriteLine("\nTotal Tests: {0}\nTotal Failed: {1}\nTotal Exceptions: {2}\n", tests.Count(), totalFailed, exceptions.Count());
			_win.EnableStatusGetters = false;
			base.OnFinishedTests(tests, startTimes, finishTimes, durations, totalDuration, totalFailed, exceptions);
		}

		/// <summary>
		/// Gets the dupes files collection.
		/// </summary>
		/// <remarks>TODO: Get queries like below working - that reference the same collection, possibly other collections etc...</remarks>
//		[Test]
		public void GetDupesDirectoriesCollection()
		{
			DateTime lastWriteTime = DateTime.Now - TimeSpan.FromDays(30);
			IQueryable<Directory> q = dirCollection.Where(d => d.LastWriteTime > lastWriteTime);
			_bufferWriter.WriteLine(
				"Collection has {0} directories\nFound {1} directories with LastWriteTime > {2}\n",
				dirCollection.Count(), q.Count(), lastWriteTime);
		}

//		[Test]
		public void GetFilesCollection()
		{
			IQueryable<File> q = fileCollection.Where(f => f.Extension.ToLower() == ".txt");
			_bufferWriter.WriteLine("Found {0} files with extension \".txt\"", q.Count());
		}
		
		/// <summary>
		/// Gets the dupes files collection.
		/// </summary>
		/// <remarks>TODO: Get queries like below working - that reference the same collection, possibly other collections etc...</remarks>
//		[Test]
		public void GetBigFilesCollection()
		{
			IQueryable<File> q = fileCollection.Where(f => f.Size > 1 * 1024 * 1024 * 1024);
//			ExpressionPrettyPrinter.PrettyPrint(
			_bufferWriter.WriteLine("Queryable \"{0}\": {1} files with Size > 1GB\n", q.Expression, q.Count());
			foreach (File f in q)
				_bufferWriter.WriteLine(f);
		}
		
//		[Test]
		public void GetSmallFilesWithoutCRC()
		{
			IQueryable<File> q = fileCollection.Where(f => f.Size < 1024 && f.CRC == null);
			_bufferWriter.WriteLine("Queryable \"{0}\": {1} files <1kB with null CRC\n", q.Expression, q.Count());
			foreach (File f in q)
			{
				try
				{
					_bufferWriter.WriteLine(f);
					FileCRC.WaitForQueueCount(4);
					f.DoCRC(false, _f => {
						_bufferWriter.WriteLine(_f);
						_client.Put(Artefact.Cache.GetArtefact(_f));	//new Artefact(_f));
//						_bufferWriter.WriteLine("{0} items queued in file CRC queue", FileCRC.QueueCount);
					});
				}
				catch (Exception ex)
				{
					_bufferWriter.WriteLine(ex);
				}
			}
			FileCRC.WaitForEmptyQueue();
			_bufferWriter.WriteLine("Queryable \"{0}\": {1} files <1kB with null CRC", q.Expression, q.Count());	// Confirm q.Count re-enumerables query
		}
		
//		[Test]
		public void GetBigFilesWithoutCRC()
		{
			IQueryable<File> q = fileCollection.Where(f => f.Size > 1 * 1024 * 1024 * 1024 && f.CRC == null);
			_bufferWriter.WriteLine("\nQueryable \"{0}\": {1} files >1GB with null CRC", q.Expression, q.Count());
			foreach (File f in q)
			{
				try
				{
					_bufferWriter.WriteLine(f);
					bool ready = false;
					int retryAttempt = 0;
					while (!FileCRC.WaitForQueueCount(2, 4000 * (retryAttempt == int.MaxValue ? retryAttempt : ++retryAttempt)))
						_bufferWriter.WriteLine("FileCRC.WaitForQueueCount(2, " + (4000 * retryAttempt) + ")");
					f.DoCRC(false, _f => {
						_bufferWriter.WriteLine("CRC {0}: {1}", _f.HasCRC ? "Success" : "Failed", _f);
						if (_f.HasCRC)
							_client.Put(Artefact.Cache.GetArtefact(_f));
//						_bufferWriter.WriteLine("{0} items queued in file CRC queue", FileCRC.QueueCount);
					});
				}
				catch (Exception ex)
				{
					_bufferWriter.WriteLine(ex);
				}
			}
			FileCRC.WaitForEmptyQueue();
			_bufferWriter.WriteLine("\nQueryable \"{0}\": {1} files >1GB with null CRC", q.Expression, q.Count());	// Confirm q.Count re-enumerables query
		}	

		[Test]
		public void RecurseFileSystem()
		{
			Directory root = new Directory(_win.RootFolder);
			long dirCount, dirTestedCount, fileCount, fileTestedCount;
			bool running = true;
			int fcrc = 0;
			Task postTask = new Task(
			() =>
			{
				while (running || PostQueue.Count > 0 || FileCRC.QueueCount > 0 || Volatile.Read(ref fcrc) > 0)
				{
					int idleLoops = 0;
					FileSystemEntry fse;
					if (PostQueue.TryDequeue(out fse))
					{
//						for (int retries = 0; retries < 3; retries++)
//						{
//							try
//							{
								File file = fse as File;
								if (file != null)
						{
//							fcrc++;
//							file.DoCRC(false, (f) => 
//							        {
//								_bufferWriter.WriteLine("Finished file {0}", f);
								fileCollection.Insert(file);
//								fcrc--;
//							});
						}
								else 
									dirCollection.Insert(fse as Directory);
//								break;
//							}
//							catch (Exception ex)
//						}
						//Thread.Sleep(2);//try avoid comms errors
					}
					else
					{
						Thread.Sleep(100 * 1<<idleLoops++);
						if (idleLoops > 5)
							idleLoops = 5;
					}
				}
			});
			postTask.Start();
			root.Recurse(
				(dir) =>
				{
					_bufferWriter.WriteLine("Queueing directory \"{0}\"", dir.Path, 
						DirectoryList.TryAdd(dir, true));
					PostQueue.Enqueue(dir);
					return true;
				},
				(dir) =>
				{
					bool v;
					_bufferWriter.WriteLine("Finished directory {0}", dir, 
						DirectoryList.TryRemove(dir, out v));
				},
				(file) => 
			{
				_bufferWriter.WriteLine("Queueing file {0}", file); 
				Interlocked.Increment(ref fcrc);
				file.DoCRC(false, (f) =>
	         	{
					_bufferWriter.WriteLine("Finished file {0}", file);
					PostQueue.Enqueue(file);
					Interlocked.Decrement(ref fcrc);
				});
				return true;
			},
			out dirCount, out dirTestedCount, out fileCount, out fileTestedCount);
			running = false;
			_bufferWriter.WriteLine("Waiting on post queue...");
			postTask.Wait();
			_bufferWriter.WriteLine("Done!");
		}
		
		/// <summary>
		/// Gets the files with matching CRC. Below is the first implementation of this test that didn't work due to nested queries.
		/// This is second strategy/attempt
		/// </summary>
		[Test] 
		public void GetFilesWithMatchingCRC()
		{
			IQueryable<File> fCRC = fileCollection.Where(f => f.CRC != null);
			_bufferWriter.WriteLine("{0} files with CRC", fCRC.Count());
			
			IEnumerable<IGrouping<string, File>> fgCRC = fCRC.AsEnumerable().GroupBy(f => f.CRC.ToString() + ":" + f.Size.ToString());
			long toolbarTotalSize = 0;
			foreach (IGrouping<string, File> fg in fgCRC)
			{
				if (fg.Count() > 1)
				{
					ArtefactCollection<File> CRCMatches = new ArtefactCollection<File>(_client, "PotentialFileDupes-" + fg.Key);
					try 
					{
						long[] vals = fg.Key.Split(':').Select(s => long.Parse(s)).ToArray();
						_bufferWriter.WriteLine("Group Total Size: {0}, CRC: {1}, Size: {2}, Count: {3}",
							(vals[1] * fg.Count()).FormatSize(), vals[0], vals[1].FormatSize(), fg.Count());
					}
					catch (FormatException fex)
					{
						Log.Error(new ApplicationException(string.Format("Failed to parse all values of \"{0}\"", fg.Key), fex));
						continue;
					}
					
					foreach (File f in fg)
					{
						_bufferWriter.WriteLine("\t{0}", f);
						int retries = 0;
						while (true)
						{
							try
							{
								CRCMatches.Insert(f);	// TODO: Test override that takes IEnumerable<T> items asparameter 
								break;
							}
							catch (WebException wex)
							{		
								Log.Error(string.Format("Error on CRCMatches.Insert(\"{0}\")", f.Path), wex);
								if (++retries == 3)
								{
									Log.Error(new ApplicationException(string.Format("Failed 3 retries on CRCMatches.Insert(\"{0}\")", f.Path)));
									break;
								}	
								else
								{
									int wait = 100 * 2 ^ retries;
									Log.WarnFormat("Failed attempt #{0} on CRCMatches.Insert(\"{1}\"): Waiting {2}ms", retries, f.Path, wait);
									Thread.Sleep(wait);
								}
							}
						}
						toolbarTotalSize += f.Size;
						_win.ToolbarTotalSize = toolbarTotalSize.FormatSize();
					}
				}
			}
			
			// 2nd try.. Ifthatdon't work maybe .Aggregate?
//			long[] allCRCs = .Where(f => f.CRC != null).Select(f => f.CRC.Value).Distinct().ToArray();
//			_bufferWriter.WriteLine("Got {0} distinct CRC values", allCRCs.Length);
//			int[] CRCCounts = new int[allCRCs.Length];
//			List<IQueryable<File>> filesMatchingCRCs = new List<IQueryable<File>>();
//			int i = 0;
//			foreach (long crc in allCRCs)
//			{
//				CRCCounts[i] = fileCollection.Count(f => f.CRC == crc);
//				if (CRCCounts[i] > 1)
//				{
//					_bufferWriter.WriteLine("CRC {0}: Count {1}", crc, CRCCounts[i]);
//					IQueryable<File> files = fileCollection.Where(f => f.CRC == crc);
//					foreach (File file in files)
//						_bufferWriter.WriteLine("\t{0}", file.Path);//CRCMatches.Insert(f);	// TODO: Test override that takes IEnumerable<T> items asparameter 
//					filesMatchingCRCs.Add(files);
//					// TODO: Test c'tor thattkaes IEnumerable<T> newItems as parameter and calls Insert()
//					ArtefactCollection<File> CRCMatches = new ArtefactCollection<File>(_client, "PotentialFileDupes-" + crc);
//					CRCMatches.Insert(files);
//				}
//				i++;
//			}
		}
		
//		[Test]
//		public void GetFilesWithMatchingCRC()
//		{
//			// TODO: I want to get the below "compound"/nested query working... cani?
//			IQueryable<File> q = fileCollection.Where(f => /* f.Size > 4 * 1024 * 1024 && */ fileCollection.Count(f2 => f2.CRC == f.CRC) > 2);
//			IQueryable<File> qEmptyCRCFiles = q.Where(f => f.CRC == null);
//			IQueryable<File> qNonEmptyCRCFiles = q.Where(f => !f.In(qEmptyCRCFiles/*.ToEnumerable()*/)); // Confirm that w/o AsEnum() inner In() enumerates qEmptyCRCFiles multiple times
//			
//			_bufferWriter.WriteLine("Queryable \"{0}\": {1} files with CRC matching >1 files(self inclusive)", q.Expression, q.Count());
//			_bufferWriter.WriteLine("Queryable \"{0}\": {1} files with null CRC", qEmptyCRCFiles.Expression, qEmptyCRCFiles.Count());
//			foreach (File f in qEmptyCRCFiles)
//				_bufferWriter.WriteLine("\t{0}", f);
//			_bufferWriter.WriteLine("Queryable \"{0}\": {1} files with non-null CRC", qNonEmptyCRCFiles.Expression, qNonEmptyCRCFiles.Count());
//			foreach (IGrouping<string, File> fg in qNonEmptyCRCFiles.GroupBy(f => f.CRC))
//			{
//				// TODO: Test c'tor thattkaes IEnumerable<T> newItems as parameter and calls Insert()
//				ArtefactCollection<File> CRCMatches = new ArtefactCollection<File>(_client, "PotentialFileDupes-" + fg.Key);
//				_bufferWriter.WriteLine("\tCRC == {0}, Count:{1}", fg.Key, fg.Count());
//				foreach (File f in fg)
//				{
//					_bufferWriter.WriteLine("\t\t{0}", f);
//					CRCMatches.Insert(f);	// TODO: Test override that takes IEnumerable<T> items asparameter 
//				}
//			}
//			_bufferWriter.WriteLine();
//			
//			foreach (File f in q)
//			{
//				try
//				{
//					_bufferWriter.WriteLine(f);
//					bool ready = false;
//					int retryAttempt = 0;
//					while (!FileCRC.WaitForQueueCount(2, 4000 * (retryAttempt == int.MaxValue ? retryAttempt : ++retryAttempt)))
//						_bufferWriter.WriteLine("FileCRC.WaitForQueueCount(2, " + (4000 * retryAttempt) + ")");
//					f.DoCRC(false, _f => {
//						_bufferWriter.WriteLine("CRC {0}: {1}", _f.HasCRC ? "Success" : "Failed", _f);
//						if (_f.HasCRC)
//							_client.Put(Artefact.Cache.GetArtefact(_f));
//						//						_bufferWriter.WriteLine("{0} items queued in file CRC queue", FileCRC.QueueCount);
//					});
//				}
//				catch (Exception ex)
//				{
//					_bufferWriter.WriteLine(ex);
//				}
//			}
//		}
	}
}

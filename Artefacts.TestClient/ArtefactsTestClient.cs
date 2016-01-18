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
using MongoDB.Bson.Serialization;

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
			
			Artefact.ConfigureServiceStack();

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
		}
		
		public override void Dispose()
		{
			_client.Dispose();
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
			int postThreadSleepTime = 334;
			int scanThreadSleepCountLimit = 5;
			int scanThreadSleepTime = 132;
			int scanThreadActiveCount = 0;
			int maxPostRetries = 3;
			ConcurrentQueue<FileSystemEntry> _delQueue = new ConcurrentQueue<FileSystemEntry>();
			ConcurrentQueue<FileSystemEntry> _postQueue = new ConcurrentQueue<FileSystemEntry>();
			ConcurrentQueue<Directory> _directoryQueue = new ConcurrentQueue<Directory>();
			
			Directory topDir = new Directory("/mnt/Trapdoor/media/");
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
				while (!MainClass.HasQuit() && (_postQueue.Count > 0 || Volatile.Read(ref scanThreadActiveCount) > 0))
				{
					if (_postQueue.Count == 0)
						Thread.Sleep(postThreadSleepTime);
					else
					{
						while (!MainClass.HasQuit() && _postQueue.TryDequeue(out fsEntry))
						{
							_win.PostQueueCount = _postQueue.Count;
//							_win.CRCQueueCount = File.CRCQueueCount;
							for (int i = 0; !MainClass.HasQuit() && (i < maxPostRetries); i++)
							{
								try 
								{
									File file = fsEntry as File;
									Directory dir = fsEntry as Directory;
									if (file != null)
									{
										if (!file.HasCRC && file.IsCRCQueued)
										{
											_bufferWriter.WriteLine("Still waiting for CRC for \"{0}\", requeueing for POST", file.Path);
											_postQueue.Enqueue(file);
											_win.PostQueueCount = _postQueue.Count;
											if (_postQueue.Count <= 32)
												Thread.Sleep(1600 / _postQueue.Count);	// give it a chance to calc, don't just thrash this thread
										}
										else
										{
											_bufferWriter.WriteLine("Creating (or updating) File \"{0}\"", file.Path);
											_client.GetOrCreate<File>((f) => f.Path == file.Path, () => file);
										}
									}
									else if (dir != null)
									{
										_bufferWriter.WriteLine("Creating (or updating) Directory \"{0}\"", dir.Path);
										_client.GetOrCreate<Directory>((d) => d.Path == dir.Path, () => dir);
									}
									else
										throw new ApplicationException("fsEntry is not a File or Directory! Shouldn't happen!");
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

			// Multiple dir scanning threads
			for (int tn = 0; !MainClass.HasQuit() && tn < numThreads; tn++)
			{
				
				// Directory scanning thread takes directories from a queue, processes sub dirs and files in that dir
				threads[tn] = new Thread((param) =>
				{
					int scanThreadSleepCount = 0;
					int num = (int)param;
					int count = 0;
					DateTime threadStart = DateTime.Now;
					_bufferWriter.WriteLine("Thread #" + num + " starting at " + threadStart.ToShortTimeString());	

					try
					{
						Directory threadDir;
						
						// Exit if main app quitting, OR if the directory queue has been empty and this thread has slept for
						// the preset number of loops AND the directory queue is empty AND there are no more of this
						// directory scanning threads actively running
						while (	!MainClass.HasQuit() &&
							(	scanThreadSleepCount++ < scanThreadSleepCountLimit
						 	 ||	_directoryQueue.Count > 0
						 	 || Volatile.Read(ref scanThreadActiveCount) > 0) )
						{
							
							// If no directories to dequeue, snooze for a bit. If this happens a preset
							// number of times, the above while loop will exit (Should it only exit if
							// the dir queue is empty and all threads have stopped?? ..)
							if (_directoryQueue.Count == 0)
								Thread.Sleep(scanThreadSleepTime);
							
							// Get a directory to scan
							else
							{
								scanThreadSleepCount = 0;
								
								// Check again the app hasn't (since last check) decided to quit and that one of the
								// other dir scanning threads hasn't emptied the queue
								while (!MainClass.HasQuit() && _directoryQueue.TryDequeue(out threadDir))
								{
									_win.DirectoryQueueCount = _directoryQueue.Count;
//									_win.CRCQueueCount = File.CRCQueueCount;
									
									// Records number of active threads so even if dir queue is empty temporarily all
									// threads won't exit straight away and leave one to do all the work
									Volatile.Write(ref scanThreadActiveCount, Volatile.Read(ref scanThreadActiveCount) + 1);
									
									try
									{	
										// TODO: The way dirs/files are retrieved from DB below is inefficient as dirs will
										// be retrieved twice if teh FS is being descended . Hence they should be cached somehow,
										// so retrieved once. I want this functionality generally in Artefacts so consider how this could be done.
										// WHich still probably means I need to rationalise how to access the artefact metadata (timestamps etc)
										// as well as specific members for each Artefact, strongly typed as e.g. File or Directory
										// and THEN how it can all be cached and tracked - client side artefact instances etc (Artefact and AType)
										// Maybe this can be integrated in a client side implementation of IEnumerable / IQueryable representing
										// a DB collection (seems most logical way, maybe they could represent >1 collection or >=1 query result,
										// stored client or server side)
										// Should maybe get all dirs at start of program. There could be lots though (in my FS tests I have >4000 )
										// so it would need to do paging, and/or retrieve in background, and/or only get artefact ID's and possibly
										// desired members per query until trying to retrieve any other members
										//
										// .. yeh ...?
										
										// Check DB/service for existence of this dir already. If exists, compare time
										// saved in DB to dir modification time to decide if dir needs scanning
										Artefact dbaDirectory = _client.Get<QueryResults>(
											QueryRequest.Make<Directory>(
												directory => directory.Path == threadDir.Path
											)).FirstOrDefault();
										bool isCurrent = dbaDirectory != null && dbaDirectory.TimeSaved > threadDir.LastWriteTime;

										// Delaying this output until dir currency is checked means one output can be used and include current status
										// (if separate outputs were used they would be on separate lines or would run the risk of being interrupted by other threads)
										_bufferWriter.WriteLine("Thread #{0} running on dir #{1} \"{2}\"",
											num, count, threadDir.Path, isCurrent ? "Current" : "Expired");
										
										if (!isCurrent && threadDir.Exists())
										{
											
											if (maxDepth < 0 || threadDir.PathDepth < maxDepth - 1)
											{	
												// Process dirs
												foreach (Directory sd in threadDir.Directories)
												{
													_directoryQueue.Enqueue(sd);
													_postQueue.Enqueue(sd);
													_win.DirectoryQueueCount = _directoryQueue.Count;
													_win.PostQueueCount = _postQueue.Count;
												}
												
												// Process files
												foreach (File file in threadDir.Files)
												{
//													if (!file.Exists())
//														file.QueueCalculateCRC();
													_postQueue.Enqueue(file);
													_win.PostQueueCount = _postQueue.Count;
												}
											}
										}
										count++;
									}
									catch (Exception ex)
									{
										_bufferWriter.WriteLine(ex.Format());
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
			threads.Each(thread => thread.Join());
			_win.PostQueueCount = 0;
			_win.DirectoryQueueCount = 0;
//			_win.CRCQueueCount = File.CRCQueueCount;
			_bufferWriter.WriteLine("Joined POST thread and scanning threads");
			//File.CRCWaitThreadFinish();
			_bufferWriter.WriteLine("File CRC thread finished");
			_bufferWriter.WriteLine("All threads finished at " + DateTime.Now + ", total time " + totalTime + ", " + mainCount + " directories traversed");
		}
		
		
//		[Test]
		public void GetParticularFiles()
		{
			QueryResults qr = _client.Get<QueryResults>(QueryRequest.Make<File>(f => f.Path == "/mnt/Trapdoor/media/incoming/Video/TV/Firefly/08_out_of_gas.avi"));
			foreach (Artefact a in qr)
			{
				_bufferWriter.WriteLine(a.ToString());
				File f = a.As<File>();
				_bufferWriter.WriteLine(f.FormatString());
				_bufferWriter.WriteLine("Has CRC: {0}", f.HasCRC);
			}
		}
		
//		[Test]
		public void GetLargeFilesWithDupeSize()
		{
			int maxPostRetries = 3;
			int threadWaitTime = 333;
			Dictionary<long, IEnumerable<File>> sizeGroups = new Dictionary<long, IEnumerable<File>>();	// file size keyed
			Dictionary<long, QueryResults> dupeGroups = new Dictionary<long, QueryResults>();	// file CRC keyed
			QueryRequest request =
				QueryRequest.Make<File>(f => 
					//f.Path.StartsWith("/mnt/Trapdoor/media/mp3/") &&
					(f.Size  > (16 * 1024 * 1024)));
			QueryResults results = _client.Get<QueryResults>(request);
			_bufferWriter.WriteLine(string.Format("{0} file results >= 16MB ", results.Count));
			long i = 0;
			long totalSize = 0;
		
			//IEnumerable<File> fileResults = results.Select(a => a.As<File>());
			
//			ParallelLoopResult result = Parallel.ForEach(
//				results.Artefacts,
//				new ParallelOptions() {
//					MaxDegreeOfParallelism = 4
//				},
//				(artefact) => {
			foreach (Artefact artefact in results.Artefacts)
			{
				long groupSize = 0;
				File file = artefact.As<File>();
				
//				_win.CRCQueueCount = File.CRCQueueCount;
				
				if (!MainClass.HasQuit() && !sizeGroups.ContainsKey(file.Size))
				{
					QueryResults results2;
					IEnumerable<File> files;
					using (new CriticalRegion())
					{
						if (sizeGroups.ContainsKey(file.Size))
							continue;
						results2 = _client.Get(QueryRequest.Make<File>(f => f.Size == file.Size));
//						results2 = new List<File>(
//							results.Artefacts
//								.Select(a => a.As<File>())
//								.Where(f => f.Size == file.Size && System.IO.File.Exists(f.Path)));
						files = results2.Artefacts.Select(a => a.As<File>());
						sizeGroups.Add(file.Size, files);
					}
					
					if (!MainClass.HasQuit() && results2.Count > 1)
					{
						_bufferWriter.WriteLine(
							"result #{0}: f.Size = {1} ({2} file results with matching size)",
							i++, File.FormatSize(file.Size), results2.Count);
//						files.Each(f => {
//							if (!MainClass.HasQuit() && !f.HasCRC)
//								f.QueueCalculateCRC(true);
//						});
						foreach (File file2 in files)
						{
							if (MainClass.HasQuit())
								return;
							
//							_win.CRCQueueCount = File.CRCQueueCount;
							
							bool calculatedCRC = !file2.HasCRC;
							if (!file2.HasCRC)
//								file2.QueueWaitCalculateCRC();
							
//							_win.CRCQueueCount = File.CRCQueueCount;

							if (!file2.HasCRC)
								_bufferWriter.WriteLine("    " + "ERROR!!".PadLeft(26) + "    " + File.FormatSize(file2.Size).PadLeft(12) + "    " + file2.Path);
								//_bufferWriter.WriteLine("ERROR! Calculating CRC for \"{0}\"", file2.Path);
							else
							{
								using (new CriticalRegion())
								{
//									long crc = file2.CRC.Value;
								long crc = file2.CRC;
									if (!dupeGroups.ContainsKey(crc))
										dupeGroups.Add(crc, new QueryResults(new Artefact[] { Artefact.Cache.GetArtefact(file2) }));	//new Artefact[] { artefact2 }));
									else
										dupeGroups[crc].Add(Artefact.Cache.GetArtefact(file2));
								}
								if (calculatedCRC)
								{
									for (int j = 0; !MainClass.HasQuit() && (j < maxPostRetries); j++)
									{
										try
										{
											_client.Put(Artefact.Cache.GetArtefact(file2));
											break;
										}
										catch (System.Net.WebException ex)
										{
											Log.ErrorFormat(" ! ERROR: {0}: Retry attempt #{1} of {2}\n", ex.GetType().FullName, j + 1, maxPostRetries);
											Thread.Sleep(500);
											if (j == maxPostRetries)
												_bufferWriter.WriteLine("Failed to PUT 3 times for file \"" + file2.Path + "\" !!");
											else
												continue;
										}
									}
								}
								_bufferWriter.WriteLine("    " + file2.CRC.ToHex().PadLeft(26) + (calculatedCRC ? "*" : " ") + "    " + File.FormatSize(file2.Size).PadLeft(12) + "    " + file2.Path);
								groupSize += file2.Size;
//								_win.CRCQueueCount = File.CRCQueueCount;
							}
						}
						_bufferWriter.WriteLine("Total " + File.FormatSize(groupSize) + " in group\n");	// results2.Artefacts.Sum(a => a.As<File>().Size)
						totalSize += groupSize;
//						_win.CRCQueueCount = File.CRCQueueCount;
					}
				}
			}//);
			
//			if (!result.IsCompleted)
//			{
//				_bufferWriter.WriteLine("Parallel.Foreach() not complete... waiting");
//				result.AsTaskResult().Wait();
//			}
			// Should implicitly wait for all tasks
//			while (!result.IsCompleted)
//				Thread.Sleep(threadWaitTime);
			_bufferWriter.WriteLine("Total " + File.FormatSize(totalSize) + " in all groups\n");
			
			_win.PostQueueCount = 0;
			_win.DirectoryQueueCount = 0;
//			_win.CRCQueueCount = File.CRCQueueCount;
			
			totalSize = 0;
			IList<IList<File>> _fileListList = new List<IList<File>>();
			foreach (KeyValuePair<long, QueryResults> qrPair in dupeGroups)
			{
				QueryResults qr = qrPair.Value;
				if (qr.Count > 1)
				{
					IList<File> _fileList = new List<File>();
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
						_fileList.Add(file);
					}
					_bufferWriter.WriteLine("Total " + File.FormatSize(groupSize) + " in group");
					totalSize += groupSize;
					_fileListList.Add(_fileList);
//					Gtk.Application.Invoke(
//						(sender, e) => {
//							DupeProcessWindow _dupeWin = new DupeProcessWindow(
//								qr.Artefacts.Select<Artefact,File>(a => a.As<File>()),
//								_win.DefaultTrashFolder, _bufferWriter);
//							_dupeWin.Show();
//						});
//					_win.Add(_dupeWin);
					
				}
			}
			Gtk.Application.Invoke(
				(sender, e) => {
					DupeProcessWindow _dupeWin = new DupeProcessWindow(_fileListList, _win.DefaultTrashFolder, _bufferWriter);
					_dupeWin.Show();
				});
			int totalUsedGroups = dupeGroups.Count(pair => pair.Value.Count > 1);
			_bufferWriter.WriteLine("Total " + File.FormatSize(totalSize) + " in " + totalUsedGroups + "/" + dupeGroups.Count + " groups");
		}
		
//		[Test]
		public void GetFilesCollection()
		{
			ArtefactCollection<File> collection = new ArtefactCollection<File>(_client, "Artefacts_FileSystem_File");
			IQueryable<File> q = collection.Where(f => f.Extension.ToLower() == ".txt");
			foreach (File f in q)
				_bufferWriter.WriteLine(f);
			_bufferWriter.WriteLine("Count: " + q.Count());
		}
		
		/// <summary>
		/// Gets the dupes files collection.
		/// </summary>
		/// <remarks>TODO: Get queries like below working - that reference the same collection, possibly other collections etc...</remarks>
		[Test]
		public void GetDupesFilesCollection()
		{
			ArtefactCollection<File> collection = new ArtefactCollection<File>(_client, "Artefacts_FileSystem_File");
			IQueryable<File> q = collection.Where(f => f.Size > (16*1024*1024) && collection.Count(f2 => f2.Size == f.Size) > (Int64)1);
			foreach (File f in q)
				_bufferWriter.WriteLine(f);
			_bufferWriter.WriteLine("Count: " + q.Count());
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
			catch (Exception ex)
			{
				_bufferWriter.WriteLine(ex.Format());
			}
		}
		
//			catch (ServiceStack.WebServiceException wsex)
//			{ 
//				_bufferWriter.Write(
//					string.Format("\nError: {0}: {1}\nStatus: {2}: {3}\nResponse: {4}\n{5}: {6}\n",
//				              wsex.ErrorCode, wsex.ErrorMessage, wsex.StatusCode, wsex.StatusDescription, wsex.ResponseBody,
//				              !string.IsNullOrWhiteSpace(wsex.ServerStackTrace) ? "ServerStackTrace" : "StackTrace",
//				              !string.IsNullOrWhiteSpace(wsex.ServerStackTrace) ? wsex.ServerStackTrace : wsex.StackTrace));
//				for (Exception _wsex = wsex.InnerException; _wsex != null; _wsex = _wsex.InnerException)
//					_bufferWriter.Write("\n" + _wsex.ToString());
//				_bufferWriter.Write("\n");
//			}
//			catch (InvalidOperationException ioex)
//			{
//				_bufferWriter.Write("\n" + ioex.ToString() + ioex.TargetSite.ToString());
//				for (Exception _wsex = ioex.InnerException; _wsex != null; _wsex = _wsex.InnerException)
//					_bufferWriter.Write("\n" + _wsex.ToString());
//				_bufferWriter.Write("\n");
//			}
//			catch (Exception ex)
//			{
//				_bufferWriter.Write("\n" + ex.ToString());	
//				for (Exception _wsex = ex.InnerException; _wsex != null; _wsex = _wsex.InnerException)	
//					_bufferWriter.Write("\n" + _wsex.ToString());
//				_bufferWriter.Write("\n");
//			}
//			finally
//			{
//				_bufferWriter.Write("\n");
//			}
		#endregion
		
		#region Old inactive and/or failed/abandoned tests

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
//		public void GetOrCreateDriveOld()
//		{
//			foreach (Drive disk in Drive.All)
//			{
//				Drive testDrive = _artefactsClient.GetOrCreate<Drive>(d => (d.Name == disk.Name), () => disk);
//				_bufferWriter.WriteLine("testResult = " + testDrive.FormatString());
//			}
//		}
		//		[Test]
		
		
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

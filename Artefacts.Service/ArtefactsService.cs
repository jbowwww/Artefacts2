using System;
using Artefacts;
using MongoDB.Bson;
using System.IO;
using System.Web;
using System.Net;
using ServiceStack;
using System.Runtime.CompilerServices;

namespace Artefacts.Service
{
	public class ArtefactsService : ServiceStack.Service//, IPut<Artefact>
	{
		private TextWriter _output = null;
		
		public ArtefactsService(TextWriter output)
		{
			_output = output;
			_output.WriteLine("Testing");
		}
		
		
//		public object Put(byte[] artefact)
//		{
//			_output.WriteLine("byte[] artefact: " + artefact.ToString());
//			//			return null;
//			return default(HttpWebResponse);
//		}
//
//		public object Put(string artefact)
//		{
//			_output.WriteLine("string artefact: " + artefact.ToString());
//			//			return null;
//			return default(HttpWebResponse);
//		}

		public object Put(BsonDocument artefact)
		{
//			try
//			{
				_output.WriteLine("BsonDocument artefact: " + artefact.ToString());
			return null;
//			}
//			
//			catch (Exception ex)
//			{
//				_output.WriteLine(ex.ToString());
//			}
			return null;
//			return default(HttpWebResponse);
		}
		
//		public object Put(Artefact artefact)
//		{
//		try
//			{
//				_output.WriteLine("Artefact artefact: " + artefact.ToString());
//			return null;
//		}
//
//		catch (Exception ex)
//		{
//			_output.WriteLine(ex.ToString());
//		}
//			return null;
//			return default(HttpWebResponse);
//		}
		
		public object Put(Artefact artefact)
					{
					try
						{
							_output.WriteLine("Artefact artefact: " + artefact.ToString());
						return null;
					}
			
					catch (Exception ex)
					{
						_output.WriteLine(ex.ToString());
					}
						return null;
						return default(HttpWebResponse);
					}
//		public object Put(object artefact)
//		{
//			_output.WriteLine("object artefact: " + artefact.ToString());
//			//			return null;
//			return default(HttpWebResponse);
//		}
		
		public object Any(object request)
		{
//			try
//			{
				_output.WriteLine("Any ! request: " + request.ToString());
			return default(HttpWebResponse);
//			}
//
//		catch (Exception ex)
//		{
//			_output.WriteLine(ex.ToString());
//		}
			return null;
		}
	}
}


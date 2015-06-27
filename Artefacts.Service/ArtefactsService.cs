using System;
using Artefacts;
using MongoDB.Bson;
using System.IO;
using System.Web;
using System.Net;

namespace Artefacts.Service
{
	public class ArtefactsService : ServiceStack.IService
	{
		private TextWriter _output = null;
		
		public ArtefactsService(TextWriter output)
		{
			_output = output;
			_output.WriteLine("Testing");
		}
		
		
		public HttpWebResponse Put(byte[] artefact)
		{
			_output.WriteLine("byte[] artefact: " + artefact.ToString());
			//			return null;
			return default(HttpWebResponse);
		}

		public HttpWebResponse Put(string artefact)
		{
			_output.WriteLine("string artefact: " + artefact.ToString());
			//			return null;
			return default(HttpWebResponse);
		}

public HttpWebResponse Put(BsonDocument artefact)
		{
			_output.WriteLine("BsonDocument artefact: " + artefact.ToString());
//			return null;
			return default(HttpWebResponse);
		}
		
		public HttpWebResponse Put(Artefact artefact)
		{
			_output.WriteLine("Artefact artefact: " + artefact.ToString());
//			return null;
			return default(HttpWebResponse);
		}
	}
}


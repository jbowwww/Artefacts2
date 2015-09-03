using System;
using Artefacts;
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

namespace Artefacts.Service
{
	public class ArtefactsService : ServiceStack.Service//, IPut<Artefact>
	{
		private TextWriter _output = null;
		
		public ArtefactsService(TextWriter output)
		{
			_output = output;
//			JsConfig<Expression>.DeSerializeFn = s => new ExpressionSerializer(new Serialize.Linq.Serializers.JsonSerializer()).DeserializeText(s);
			JsConfig<Artefact>.SerializeFn = a => a.Data.SerializeToString();
			JsConfig<Artefact>.DeSerializeFn = a => new Artefact() { Data = a.FromJson<DataDictionary>() };

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
		
		public object Put(DataDictionary data)
		{
			try
			{
				_output.WriteLine("DataDictionary data: " + data.Dump());
				return null;
			}

			catch (Exception ex)
			{
				_output.WriteLine(ex.ToString());
			}
			return null;
			//			return default(HttpWebResponse);e
		}

		public object Put(BsonDocument artefact)
		{
			try
			{
				_output.WriteLine("BsonDocument artefact: " + artefact.ToString());
			return null;
			}
			
			catch (Exception ex)
			{
				_output.WriteLine(ex.ToString());
			}
//			return null;
			return default(HttpWebResponse);
		}
		
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
	//			return default(HttpWebResponse);e
		}
		
//		public object Put(Artefact artefact)
//					{
//					try
//						{
//				_output.WriteLine("Artefact artefact\n{\n\tpublic class ArtefactsService : ServiceStack.Service, IPut<Artefact>\n\t{\n\t\tprivate TextWriter _output = null;\n\t\t: " + ((Artefact)artefact).ToString());
//						return null;
//					}
//			
//					catch (Exception ex)
//					{
//						_output.WriteLine(ex.ToString());
//					}
////						return null;
//						return default(HttpWebResponse);
//					}
//		public object Put(object artefact)
//		{
//			_output.WriteLine("object artefact: " + artefact.ToString());
//			//			return null;
//			return default(HttpWebResponse);
//		}
		
		public Artefact Get(MatchArtefactRequest request)
		{
			try {
				_output.WriteLine(request.Match != null ? request.Match.ToString() : "(null)");
			}
			catch(Exception ex)
			{
				;
			}
			return null;
		}
		
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


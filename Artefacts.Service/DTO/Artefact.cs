using System;
using ServiceStack;

namespace Artefacts.Service.DTO
{
	[Route("/artefacts/{Id}")]
	public class Artefact
	{
		public string Id;
		
		public Uri Uri;
		
		public Artefact()
		{
		}
	}
}


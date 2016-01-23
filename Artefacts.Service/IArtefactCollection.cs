using System;
using ServiceStack;

namespace Artefacts.Service
{
	public interface IArtefactCollection
	{
		IServiceClient Client { get; }
		string Name { get; }
	}
}


using System;
using ServiceStack;
using System.Linq;

namespace Artefacts.Service
{
	public interface IArtefactCollection : IQueryProvider
	{
		IServiceClient Client { get; }
		string Name { get; }
	}
}


using System;
using ServiceStack;
using System.Linq.Expressions;

namespace Artefacts.Service.Extensions
{
	public static class ServiceClient_Extensions
	{
		public static Artefact GetOrCreate<T>(this IServiceClient client, Expression<Func<T, bool>> predicate, Func<T> create)
		{
			Artefact artefact;
			QueryResults results = client.Get<QueryResults>(QueryRequest.Make<T>(typeof(T).FullName, predicate));
			if (results.Count > 0)
				artefact = results.Artefacts[0];
			else
			{
				T instance = create();
				artefact = Artefact.Cache.GetArtefact(instance);	// new Artefact(create());
				client.Post(artefact);
			}
			return artefact;
		}
	}
}


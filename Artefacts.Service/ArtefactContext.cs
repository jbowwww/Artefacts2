using System;

namespace Artefacts.Service
{
	/// <summary>
	/// Artefact context.
	/// </summary>
	public class ArtefactContext : IDisposable
	{
		#region Consturction / Destruction
		/// <summary>
		/// Initializes a new instance of the <see cref="Artefacts.TestClient.ArtefactContext"/> class.
		/// </summary>
		public ArtefactContext()
		{
		}

		/// <summary>
		/// Releases all resource used by the <see cref="Artefacts.TestClient.ArtefactContext"/> object.
		/// </summary>
		/// <remarks>
		/// IDisposable implementation
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="Artefacts.TestClient.ArtefactContext"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Artefacts.TestClient.ArtefactContext"/> in an unusable state.
		/// After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Artefacts.TestClient.ArtefactContext"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Artefacts.TestClient.ArtefactContext"/> was occupying.
		/// </remarks>
		public void Dispose()
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}


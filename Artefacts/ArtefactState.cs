using System;

namespace Artefacts
{
	public enum ArtefactState {
		Unknown = 0,		// Only gets set in protected c'tor which should only be called when deserializing. Once d's'ized State should becoome current so this should never be seen
		Created = 1,		// newly created Artefact(instance) (currently only sets state when constructed with instance parameter)
		Current,			// This artefact instance's data is equal to what has been pushed into the repostiroy
		Modified,			// This artefact instance's data has been modified since last pushed to repo
		Stale,				// This artefact instance's data is out of date and older than what exists in the repo
		Deleted				// This artefact instance represents an artefact that has now been deleted from the repo
	};
}


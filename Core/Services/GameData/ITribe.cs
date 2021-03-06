﻿// Concept Matrix 3.
// Licensed under the MIT license.

namespace ConceptMatrix.GameData
{
	public interface ITribe : IDataObject
	{
		Appearance.Tribes Tribe { get; }
		string Feminine { get; }
		string Masculine { get; }
		string DisplayName { get; }
	}
}

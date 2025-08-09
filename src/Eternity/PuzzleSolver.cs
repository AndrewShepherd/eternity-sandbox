namespace Eternity
{
	public static class PuzzleSolver
	{
		private static List<Rotation> GetPossibleRotations(
			Position position,
			int pieceIndex,
			Placements listPlacements
		)
		{
			var pc = listPlacements.Constraints.At(position).PatternConstraints;
			var patterns = listPlacements.PieceSides[pieceIndex];
			List<Rotation> result = new List<Rotation>();
			ulong[] rp = new ulong[4];
			foreach(var rotation in RotationExtensions.AllRotations)
			{
				RotationExtensions.Rotate(patterns, rotation, rp);
				if (
					(pc.Left & rp[EdgeIndexes.Left]) != 0
					&& (pc.Top & rp[EdgeIndexes.Top]) != 0
					&& (pc.Right & rp[EdgeIndexes.Right]) != 0
					&& (pc.Bottom & rp[EdgeIndexes.Bottom]) != 0
				)
				{
					result.Add(rotation);
				}
			}
			return result;
		}

		public static Placements? TryAddPiece(
			Placements listPlacements,
			Position position,
			int pieceIndex
			)
		{
			if (!listPlacements.Constraints.At(position).Pieces.Contains(pieceIndex))
			{
				return null;
			}
			var rotations = PuzzleSolver.GetPossibleRotations(
				position,
				pieceIndex,
				listPlacements
			);

			if (rotations.Count == 0)
			{
				return null;
			}
			return listPlacements.SetItem(
				new Placement(position, pieceIndex, rotations.ToArray())
			);
		}

	}
}

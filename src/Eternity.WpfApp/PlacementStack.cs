namespace Eternity.WpfApp
{
	internal class PlacementStack
	{
		public record class StackEntry(int PieceIndex, Placements Placements);

		public StackEntry?[] _stackEntries = new StackEntry?[256];

		// Mutates the stack so that it gets as far as it can
		// Returns the number of pieces it actually applied
		public int ApplyPieceOrder(
			PuzzleEnvironment puzzleEnvironment,
			IEnumerable<int> pieceIndexes
		)
		{
			// Find the stack entry that matches this
			var matchingStackEntryIndex = -1;
			var matchingPlacements = Placements.Empty;

			var pieceIndexEnumerator = pieceIndexes.GetEnumerator();
			int i = 0;
			for (i = 0; i < this._stackEntries.Length; ++i)
			{
				pieceIndexEnumerator.MoveNext();
				var thisEntry = this._stackEntries[i];
				if (thisEntry == null)
				{
					break;
				}
				if (thisEntry.PieceIndex != pieceIndexEnumerator.Current)
				{
					for (int j = i; j < this._stackEntries.Length; ++j)
					{
						if (this._stackEntries[j] == null)
						{
							break;
						}
						this._stackEntries[j] = null;
					}
					break;
				}
				matchingStackEntryIndex = i;
				matchingPlacements = thisEntry.Placements;
			}

			for (; true; ++i)
			{
				var newPlacements = PuzzleSolver.TryAddPiece(
					puzzleEnvironment,
					matchingPlacements,
					i,
					pieceIndexEnumerator.Current
				);
				if (newPlacements == null)
				{
					return i;
				}
				this._stackEntries[i] = new StackEntry(pieceIndexEnumerator.Current, newPlacements);
				matchingPlacements = newPlacements;
				if (!pieceIndexEnumerator.MoveNext())
				{
					break;
				}
			}
			return i;
		}
	}
}

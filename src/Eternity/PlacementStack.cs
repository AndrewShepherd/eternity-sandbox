namespace Eternity
{
	using System.Collections.Immutable;

	public class PlacementStack
	{
		public record class StackEntry(int PieceIndex, Placements Placements, IPositioner Positioner);

		public StackEntry?[] _stackEntries = new StackEntry?[256];

		// Mutates the stack so that it gets as far as it can
		// Returns the number of pieces it applied
		// and the resulting Placements
		// (Note that there may be more placements than asked for
		// because squares which can only have one piece are automatically
		// filled)

		private Placements? _initialPlacements;
		public (int, Placements) ApplyPieceOrder(
			IReadOnlyList<ImmutableArray<int>> pieceSides,
			IEnumerable<int> sequence
		)
		{
			// Find the stack entry that matches this
			var matchingStackEntryIndex = -1;
			if (_initialPlacements == null)
			{
				_initialPlacements = Placements.CreateInitial(
					pieceSides
				);
			}
			Placements matchingPlacements = _initialPlacements;
			IPositioner positioner = new DynamicPositionerAdjacentsOnly(_initialPlacements.Dimensions);

			var pieceIndexEnumerator = sequence.GetEnumerator();
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
				positioner = thisEntry.Positioner;
			}

			for (; true; ++i)
			{
				// This is the bit where the positioner
				// dynamically chooses the position
				var (nextPosition, newPositioner) = positioner.GetNext(matchingPlacements.Constraints);
				Placements? newPlacements = null;
				int pieceIndex = pieceIndexEnumerator.Current;
				var possiblePieces = matchingPlacements.Constraints.At(nextPosition).Pieces;
				if (pieceIndex < possiblePieces.Count)
				{
					var pieceId = possiblePieces.OrderBy(p => p).ElementAt(pieceIndex);
					newPlacements = PuzzleSolver.TryAddPiece(
						matchingPlacements,
						nextPosition,
						pieceId
					);
				}
				if (newPlacements == null)
				{
					if (i == 0)
					{
						return (0, _initialPlacements);
					}
					var lastPlacement = this._stackEntries[i - 1]?.Placements;
					if (lastPlacement == null)
					{
						throw new Exception("Inexplicable null entry in the stack");
					}
					return (i, lastPlacement);
				}
				this._stackEntries[i] = new StackEntry(
					pieceIndexEnumerator.Current, 
					newPlacements,
					newPositioner
				);
				matchingPlacements = newPlacements;
				positioner = newPositioner;
				if (!pieceIndexEnumerator.MoveNext())
				{
					break;
				}
			}
			return (i, this._stackEntries[pieceSides.Count - 1]!.Placements);
		}
	}
}

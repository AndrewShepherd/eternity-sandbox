namespace Eternity
{
	using System.Collections.Immutable;

	public record class StackEntry(
		int PieceIndex,
		int PossiblePieceCount, // the count of items that could go in that slot
		Position Position, // The position that this stack entry represents
		Placements? Placements,
		IPositioner Positioner
	);


	public static class StackEntryExtensions
	{
		public static int? ProgressForwards(int? startIndex, int count) =>
			startIndex switch
			{
				int n when n >= count - 1 => null,
				int n => n + 1,
				_ => 0
			};

		public static int? ProgressBackwards(int? startIndex, int count) =>
			startIndex switch
			{
				int n when n > 0 => n - 1,
				int n => null,
				_ => count - 1
			};

		private enum ProgressionState
		{
			Initial,
			AfterFirstSuccess,
			AfterFirstFailure
		};

		public static ImmutableList<StackEntry> CreateInitialStack(
			Func<int?, int, int?> progressIndex,
			IReadOnlyList<ImmutableArray<int>> pieceSides
		)
		{
			Placements initialPlacements = Placements.CreateInitial(
				pieceSides
			);
			IPositioner positioner = new DynamicPositionerAdjacentsOnly(initialPlacements.Dimensions);
			// Push the first value
			(var firstPosition, positioner) = positioner.GetNext(initialPlacements.Constraints);
			var availablePieces = initialPlacements.Constraints.At(firstPosition).Pieces;
			var pieceIndex = progressIndex(null, availablePieces.Count);
			if (pieceIndex.HasValue)
			{
				Placements? attempt = PuzzleSolver.TryAddPiece(
					initialPlacements,
					firstPosition,
					availablePieces.ElementAt(new Index(pieceIndex.Value))
				);
				if (attempt == null)
				{
					return ImmutableList<StackEntry>.Empty;
				}
				var newStackEntry = new StackEntry(
					PieceIndex: pieceIndex.Value,
					PossiblePieceCount: availablePieces.Count,
					Position: firstPosition,
					Placements: attempt,
					Positioner: positioner
				);
				return ImmutableList<StackEntry>.Empty.Add(newStackEntry);
			}
			return ImmutableList<StackEntry>.Empty;
		}

		private static ImmutableList<StackEntry> ProgressToFirstSuccess(ImmutableList<StackEntry> stack, Func<int?, int, int?> progressIndex)
		{
			while(true)
			{
				if (stack.Count <2)
				{
					return stack;
				}
				var lastEntry = stack[stack.Count - 1];
				var secondLastEntry = stack[stack.Count - 2];
				if (secondLastEntry.Placements == null)
				{
					throw new Exception("Inexplicable state");
				}
				var constraints = secondLastEntry.Placements.Constraints;
				var pieceIndex = progressIndex(lastEntry.PieceIndex, lastEntry.PossiblePieceCount);
				if (!pieceIndex.HasValue)
				{
					stack = stack.RemoveAt(stack.Count - 1);
					continue;
				}
				var availablePieces = constraints.At(lastEntry.Position).Pieces;
				Placements? attempt = PuzzleSolver.TryAddPiece(
					secondLastEntry.Placements,
					lastEntry.Position,
					availablePieces.ElementAt(new Index(pieceIndex.Value))
				);
				var stackEntry = new StackEntry(
					PieceIndex: pieceIndex.Value,
					PossiblePieceCount: lastEntry.PossiblePieceCount,
					Position: lastEntry.Position,
					Placements: attempt,
					Positioner: lastEntry.Positioner
				);
				stack = stack.Replace(lastEntry, stackEntry);
				if (attempt != null)
				{
					return stack;
				}
			}
		}

		private static ImmutableList<StackEntry> ExtendThroughDefaultSelection(ImmutableList<StackEntry> stack, Func<int?, int, int?> progressIndex)
		{
			while (true)
			{
				var lastEntry = stack[stack.Count - 1];
				if (lastEntry.Placements == null)
				{
					return stack;
				}
				if (stack.Count > lastEntry.Placements.PieceSides.Count)
				{
					throw new Exception("Error: Placed more items than possible on the stack");
				}
				if (stack.Count == lastEntry.Placements.PieceSides.Count)
				{
					return stack;
				}
				var (position, positioner) = lastEntry.Positioner.GetNext(
					lastEntry.Placements.Constraints
				);
				if (position == lastEntry.Position)
				{
					throw new Exception("Returned a position that was already used");
				}
				// Sanity check
				if ((lastEntry.Positioner as DynamicPositionerAdjacentsOnly)!._returnedAlready.Contains(position))
				{
					throw new Exception("Positioner returned a position it had already returned");
				}
				if ((positioner as DynamicPositionerAdjacentsOnly)!._adjacentPositions.Contains(position))
				{
					throw new Exception("Returned positioner has current position in its adjacent positions");
				}
				var availablePieces = lastEntry.Placements.Constraints.At(position).Pieces;
				var pieceIndex = progressIndex(null, availablePieces.Count);
				if (!pieceIndex.HasValue)
				{
					return stack;
				}
				Placements? attempt = PuzzleSolver.TryAddPiece(
					lastEntry.Placements,
					position,
					availablePieces.ElementAt(new Index(pieceIndex.Value))
				);
				stack = stack.Add(
					new(
						PieceIndex: pieceIndex.Value,
						PossiblePieceCount: availablePieces.Count,
						Position: position,
						Placements: attempt,
						Positioner: positioner
					)
				);
			}
		}

		public static ImmutableList<StackEntry> Progress(
			this ImmutableList<StackEntry> stack,
			Func<int?, int, int?> progressIndex,
			IReadOnlyList<ImmutableArray<int>> pieceSides
		)
		{
			if (stack.Count == 0)
			{
				stack = CreateInitialStack(progressIndex, pieceSides);
			}
			else if (stack.Count >= 2)
			{
				stack = ProgressToFirstSuccess(stack, progressIndex);
			}
			stack = ExtendThroughDefaultSelection(stack, progressIndex);
			return stack;
		}
	}
}

using System.Collections.Immutable;
using System.Data;
using System.Runtime.CompilerServices;
namespace Eternity
{
	public class SquareConstraint
	{
		public ImmutableHashSet<int> Pieces { get; init; } = SquareConstraintExtensions.AllPieces;
		ImmutableHashSet<int> LeftPatterns { get; init; } = SquareConstraintExtensions.AllPatterns;
		ImmutableHashSet<int> TopPatterns { get; init; } = SquareConstraintExtensions.AllPatterns;

		ImmutableHashSet<int> RightPatterns { get; init; } = SquareConstraintExtensions.AllPatterns;

		ImmutableHashSet<int> BottomPatterns { get; init; } = SquareConstraintExtensions.AllPatterns;

		public SquareConstraint()
		{
		}

		public static SquareConstraint Initial = new SquareConstraint();

		public SquareConstraint SetPlacement(
			Placement placement
		) =>
			new SquareConstraint
			{
				Pieces = new[] { placement.PieceIndex }.ToImmutableHashSet(),
				LeftPatterns = this.LeftPatterns,
				TopPatterns = this.TopPatterns,
				RightPatterns = this.RightPatterns,
				BottomPatterns = this.BottomPatterns,
			};

		public SquareConstraint RemovePossiblePiece(
			int pieceIndex
		)
		{
			if ( Pieces.Contains( pieceIndex ))
			{
				return new SquareConstraint
				{
					Pieces = this.Pieces.Remove(pieceIndex),
					LeftPatterns = this.LeftPatterns,
					TopPatterns = this.TopPatterns,
					RightPatterns = this.RightPatterns,
					BottomPatterns = this.BottomPatterns,
				};
			}
			else
			{
				return this;
			}
		}
	}
	
	public static class SquareConstraintExtensions
	{
		public static ImmutableHashSet<int> AllPieces = Enumerable.Range(0, 256).ToImmutableHashSet();

		public static ImmutableHashSet<int> AllPatterns = Enumerable.Range(0, 24).ToImmutableHashSet();



		public static ImmutableArray<SquareConstraint> SetPlacement(
			this IReadOnlyList<SquareConstraint> constraints,
			int positionIndex,
			Placement placement)
		{
			return constraints.Select(
				(c, i) =>
				{
					if (i == positionIndex)
					{
						return c.SetPlacement(placement);
					}
					else
					{
						return c.RemovePossiblePiece(placement.PieceIndex);
					}
				}
			).ToImmutableArray();
		}
	}
}

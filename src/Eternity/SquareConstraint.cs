using System.Collections.Immutable;
namespace Eternity
{
	public class SquareConstraint
	{
		public ImmutableHashSet<int> Pieces { get; init; } = SquareConstraintExtensions.AllPieces;
		ImmutableHashSet<int> LeftPatterns { get; init; } = SquareConstraintExtensions.AllPatterns;
		ImmutableHashSet<int> TopPatterns { get; init; } = SquareConstraintExtensions.AllPatterns;

		ImmutableHashSet<int> RightPatterns { get; init; } = SquareConstraintExtensions.AllPatterns;

		ImmutableHashSet<int> BottomPatterns { get; init; } = SquareConstraintExtensions.AllPatterns;

		private SquareConstraint()
		{
		}

		public static SquareConstraint Initial = new SquareConstraint();

	}
	
	public static class SquareConstraintExtensions
	{
		public static ImmutableHashSet<int> AllPieces = Enumerable.Range(0, 256).ToImmutableHashSet();

		public static ImmutableHashSet<int> AllPatterns = Enumerable.Range(0, 24).ToImmutableHashSet();
	}
}

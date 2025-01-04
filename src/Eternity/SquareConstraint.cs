using System.Collections.Immutable;
using System.Data;
using System.Runtime.CompilerServices;
namespace Eternity
{
	public record MultiPatternConstraints
	{
		public required ImmutableHashSet<int> Left;
		public required ImmutableHashSet<int> Right;
		public required ImmutableHashSet<int> Top;
		public required ImmutableHashSet<int> Bottom;

	}

	public record class SquareConstraint
	{
		public ImmutableHashSet<int> Pieces { get; init; } = SquareConstraintExtensions.AllPieces;

		public MultiPatternConstraints PatternConstraints = new()
		{
			Bottom = SquareConstraintExtensions.AllPatterns,
			Top = SquareConstraintExtensions.AllPatterns,
			Left = SquareConstraintExtensions.AllPatterns,
			Right = SquareConstraintExtensions.AllPatterns
		};

		public required IReadOnlyList<ImmutableArray<int>> PiecePatternLookup { get; init; }
		public SquareConstraint()
		{
		}

		public SquareConstraint SetPlacement(
			Placement placement
		) =>
			this with { Pieces = new[] { placement.PieceIndex }.ToImmutableHashSet() };


		private MultiPatternConstraints AdjustFilterConstraintsBasedOnAvailablePieces(
			ImmutableHashSet<int> availablePieces,
			MultiPatternConstraints currentConstraints
		)
		{
			var top = new HashSet<int>();
			var bottom = new HashSet<int>();
			var left = new HashSet<int>();
			var right = new HashSet<int>();

			foreach (var pieceIndex in availablePieces)
			{
				var piecePattern = this.PiecePatternLookup[pieceIndex];
				foreach (var rotation in RotationExtensions.AllRotations)
				{
					var rotatedPattern = RotationExtensions.Rotate(piecePattern, rotation);
					if (
						currentConstraints.Top.Contains(rotatedPattern[EdgeIndexes.Top])
						&& currentConstraints.Left.Contains(rotatedPattern[EdgeIndexes.Left])
						&& currentConstraints.Bottom.Contains(rotatedPattern[EdgeIndexes.Bottom])
						&& currentConstraints.Right.Contains(rotatedPattern[EdgeIndexes.Right])
					)
					{
						top.Add(rotatedPattern[EdgeIndexes.Top]);
						bottom.Add(rotatedPattern[EdgeIndexes.Bottom]);
						left.Add(rotatedPattern[EdgeIndexes.Left]);
						right.Add(rotatedPattern[EdgeIndexes.Right]);
					}
				}
			}
			return currentConstraints with
			{
				Bottom = currentConstraints.Bottom.Intersect(bottom),
				Top = currentConstraints.Top.Intersect(top),
				Left = currentConstraints.Left.Intersect(left),
				Right = currentConstraints.Right.Intersect(right)
			};
		}

		private ImmutableHashSet<int> FilterSetBasedOnPatterns(
			ImmutableHashSet<int> hashSet,
			MultiPatternConstraints c
		)
		{
			var rv = hashSet;
			foreach (var pieceIndex in hashSet)
			{
				var piecePattern = this.PiecePatternLookup[pieceIndex];
				bool approved = false;
				foreach (var rotation in RotationExtensions.AllRotations)
				{
					var rotatedPattern = RotationExtensions.Rotate(piecePattern, rotation);
					if (
						c.Top.Contains(rotatedPattern[EdgeIndexes.Top])
						&& c.Left.Contains(rotatedPattern[EdgeIndexes.Left])
						&& c.Bottom.Contains(rotatedPattern[EdgeIndexes.Bottom])
						&& c.Right.Contains(rotatedPattern[EdgeIndexes.Right])
					)
					{
						approved = true;
						break;
					}
				}
				if (!approved)
				{
					rv = rv.Remove(pieceIndex);
				}
			}
			return rv;
		}

		private SquareConstraint ModifyPatternConstraints(
			Func<MultiPatternConstraints, MultiPatternConstraints> transform
		)
		{
			var newConstraints = transform(this.PatternConstraints);
			if (newConstraints.Equals(this.PatternConstraints))
			{
				return this;
			}
			var newPieces = FilterSetBasedOnPatterns(this.Pieces, newConstraints);
			if (!newPieces.Equals(this.Pieces))
			{
				// Changes the PatternConstraints to match the pieces
				newConstraints = AdjustFilterConstraintsBasedOnAvailablePieces(newPieces, newConstraints);
			}
			return this with
			{
				PatternConstraints = newConstraints,
				Pieces = FilterSetBasedOnPatterns(this.Pieces, newConstraints)
			};
		}

		public SquareConstraint SetTopPattern(
			int pattern
		) => ModifyPatternConstraints(
			mp => mp with { Top = new[] { pattern }.ToImmutableHashSet() }
		);

		public SquareConstraint SetLeftPattern(
			int pattern
		) => ModifyPatternConstraints(
			mp => mp with { Left = new[] { pattern }.ToImmutableHashSet() }
		);

		public SquareConstraint SetRightPattern(
			int pattern
		) => ModifyPatternConstraints(
			mp => mp with { Right = new[] { pattern }.ToImmutableHashSet() }
		);

		public SquareConstraint SetBottomPattern(
			int pattern
		) => ModifyPatternConstraints(
			mp => mp with { Bottom = new[] { pattern }.ToImmutableHashSet() }
		);

		public SquareConstraint RemovePossiblePiece(
			int pieceIndex
		)
		{
			if ( Pieces.Contains( pieceIndex ))
			{
				return this with
				{
					Pieces = this.Pieces.Remove(pieceIndex)
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


		private static ImmutableArray<SquareConstraint> TransformConstraint(
			IReadOnlyList<SquareConstraint> constraints,
			int positionIndex,
			Func<SquareConstraint, SquareConstraint> transform
		)
		{
			var before = constraints[positionIndex];
			var after = transform(before);
			if (before != after)
			{
				// TODO. Apply all of the cascading effects
			}
			return constraints.ToImmutableArray().SetItem(positionIndex, after);
		}

		public static ImmutableArray<SquareConstraint> GenerateInitialPlacements(IReadOnlyList<ImmutableArray<int>> pieceSides)
		{
			var constraintsArray = new SquareConstraint[256];
			var initialConstraint = new SquareConstraint
			{
				PiecePatternLookup = pieceSides
			};
			for(int placementIndex = 0; placementIndex < 256; ++placementIndex)
			{
				constraintsArray[placementIndex] = initialConstraint;
			}

			var constraints = constraintsArray.ToImmutableArray();
			for (int placementIndex = 0; placementIndex < 256; ++placementIndex)
			{
				var position = Positions.PositionLookup[placementIndex];
				if (position.X == 0)
				{
					constraints = TransformConstraint(
						constraints, 
						placementIndex, c => c.SetLeftPattern(23)
					);
				}
				if (position.Y == 0)
				{
					constraints = TransformConstraint(
						constraints,
						placementIndex, c => c.SetTopPattern(23)
					);
				}
				if (position.X == 15)
				{
					constraints = TransformConstraint(
						constraints,
						placementIndex,
						c => c.SetRightPattern(23)
					);
				}
				if (position.Y == 15)
				{
					constraints = TransformConstraint(
						constraints,
						placementIndex, 
						c => c.SetBottomPattern(23)
					);
				}
			}
			return constraints;
		}



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

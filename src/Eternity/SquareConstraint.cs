using System.Collections.Immutable;
using System.Data;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
namespace Eternity
{
	public static class ImmutableHashSetExtensions
	{
		public static bool IsEquivalentTo(this ImmutableHashSet<int> left, ImmutableHashSet<int> right)
		=>
			ReferenceEquals(left, right)
			|| (
				(left.Count == right.Count)
				&& right.All(left.Contains)
			);

		public static ImmutableHashSet<int> Constrain(this ImmutableHashSet<int> s1, ImmutableHashSet<int> c)
		{
			if (s1.IsEquivalentTo(c))
			{
				return s1;
			}
			else
			{
				return s1.Intersect(c);
			}
		}
	}

	public record MultiPatternConstraints
	{
		public required ImmutableHashSet<int> Left;
		public required ImmutableHashSet<int> Right;
		public required ImmutableHashSet<int> Top;
		public required ImmutableHashSet<int> Bottom;

		public readonly static MultiPatternConstraints Never = new()
		{
			Left = ImmutableHashSet<int>.Empty,
			Bottom = ImmutableHashSet<int>.Empty,
			Right = ImmutableHashSet<int>.Empty,
			Top = ImmutableHashSet<int>.Empty
		};
	}

	public static class MultiPatterConstraintsExtensions
	{
		public static bool IsEquivalentTo(
			this MultiPatternConstraints left,
			MultiPatternConstraints right
		) =>
			left.Left.IsEquivalentTo(right.Left)
			&& left.Top.IsEquivalentTo(right.Top)
			&& left.Right.IsEquivalentTo(right.Right)
			&& left.Bottom.IsEquivalentTo(right.Bottom);
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

		public readonly static SquareConstraint Never = new()
		{
			PiecePatternLookup = ImmutableArray<ImmutableArray<int>>.Empty,
			PatternConstraints = MultiPatternConstraints.Never,
			Pieces = ImmutableHashSet<int>.Empty
		};

		public SquareConstraint SetPlacement(
			Placement placement
		)
		{
			// TODO: Update the edge constraints
			var newPatternConstraints = new MultiPatternConstraints
			{
				Left = ImmutableHashSet<int>.Empty,
				Bottom = ImmutableHashSet<int>.Empty,
				Right = ImmutableHashSet<int>.Empty,
				Top = ImmutableHashSet<int>.Empty,
			};
			var patterns = this.PiecePatternLookup[placement.PieceIndex];
			foreach(var rotation in placement.Rotations)
			{
				var rotated = RotationExtensions.Rotate(patterns, rotation);
				newPatternConstraints = new MultiPatternConstraints
				{
					Left = newPatternConstraints.Left.Add(rotated[EdgeIndexes.Left]),
					Bottom = newPatternConstraints.Bottom.Add(rotated[EdgeIndexes.Bottom]),
					Top = newPatternConstraints.Top.Add(rotated[EdgeIndexes.Top]),
					Right = newPatternConstraints.Right.Add(rotated[EdgeIndexes.Right])
				};
			}
			return this with { 
				Pieces = new[] { placement.PieceIndex }.ToImmutableHashSet(),
				PatternConstraints = newPatternConstraints
			};
		}


		private MultiPatternConstraints AdjustPatternConstraintsBasedOnAvailablePieces(
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

		private (MultiPatternConstraints, ImmutableHashSet<int>) AdjustPatternConstraintsBasedOnAvailablePiecesRecursive(
			MultiPatternConstraints patterns,
			ImmutableHashSet<int> pieces)
		{
			var newPatterns = AdjustPatternConstraintsBasedOnAvailablePieces(pieces, patterns);
			if (newPatterns.IsEquivalentTo(patterns))
			{
				return (patterns, pieces);
			}
			else
			{
				return FilterSetBasedOnPatternsRecursive(newPatterns, pieces);
			}
		}

		private (MultiPatternConstraints, ImmutableHashSet<int>) FilterSetBasedOnPatternsRecursive(
			MultiPatternConstraints patterns,
			ImmutableHashSet<int> pieces)
		{
			var newPieces = FilterSetBasedOnPatterns(pieces, patterns);
			if (newPieces.IsEquivalentTo(pieces))
			{
				return (patterns, pieces);
			}
			else
			{
				return AdjustPatternConstraintsBasedOnAvailablePiecesRecursive(
					patterns,
					newPieces
				);
			}
		}

		private SquareConstraint ModifyPatternConstraints(
			Func<MultiPatternConstraints, MultiPatternConstraints> transform
		)
		{
			var newConstraints = transform(this.PatternConstraints);
			if (newConstraints.IsEquivalentTo(this.PatternConstraints))
			{
				return this;
			}
			else
			{
				(newConstraints, var newPieces) = AdjustPatternConstraintsBasedOnAvailablePiecesRecursive(
					newConstraints,
					this.Pieces
				);
				return this with
				{
					PatternConstraints = newConstraints,
					Pieces = newPieces
				};
			}
		}

		public SquareConstraint RemovePossiblePiece(
			int pieceIndex
		)
		{
			if (Pieces.Contains(pieceIndex))
			{
				var (newConstraints, newPieces) = AdjustPatternConstraintsBasedOnAvailablePiecesRecursive(
					this.PatternConstraints,
					this.Pieces.Remove(pieceIndex)
				);
				return this with
				{
					Pieces = newPieces,
					PatternConstraints = newConstraints
				};
			}
			else
			{
				return this;
			}
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

		public SquareConstraint ConstrainLeftPattern(
			ImmutableHashSet<int> patterns
		) => ModifyPatternConstraints(
			mp => 
				mp with 
				{ 
					Left = ImmutableHashSetExtensions.Constrain(mp.Left, patterns) 
				}
		);

		public SquareConstraint ConstrainTopPattern(
			ImmutableHashSet<int> patterns
		) => ModifyPatternConstraints(
			mp => 
				mp with
				{
					Top = ImmutableHashSetExtensions.Constrain(mp.Top, patterns)
				}
		);

		public SquareConstraint ConstrainBottomPattern(
			ImmutableHashSet<int> patterns
		) => ModifyPatternConstraints(
			mp => 
				mp with 
				{ 
					Bottom = ImmutableHashSetExtensions.Constrain(mp.Bottom, patterns) 
				}
		);

		public SquareConstraint ConstrainRightPattern(
			ImmutableHashSet<int> patterns
		) => ModifyPatternConstraints(
			mp => 
				mp with 
				{ 
					Right = ImmutableHashSetExtensions.Constrain(mp.Right, patterns)
				}
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
	}
	
	public static class SquareConstraintExtensions
	{
		public static ImmutableHashSet<int> AllPieces = Enumerable.Range(0, 256).ToImmutableHashSet();

		public static ImmutableHashSet<int> AllPatterns = Enumerable.Range(0, 24).ToImmutableHashSet();


		public static bool IsEquiavelentTo(
			this SquareConstraint c1,
			SquareConstraint c2
		) =>
			ReferenceEquals(c1, c2)
			|| (
				c1.Pieces.IsEquivalentTo(c2.Pieces)
				&& c1.PatternConstraints.IsEquivalentTo(c2.PatternConstraints)
			);

		private static int? TransformPositionIndex(int positionIndex, Func<Position, Position> t)
		{
			var position = Positions.PositionLookup[positionIndex];
			var adjacentPosition = t(position);
			if(Positions.ReversePositionLookup.TryGetValue(adjacentPosition, out var result))
			{
				return new int?(result);
			}
			else
			{
				return default;
			}
		}

		private static ImmutableArray<SquareConstraint>? ProcessQueue(
			ImmutableArray<SquareConstraint> constraints,
			SquareConstraintTransformQueue q
		)
		{
			while (true)
			{
				var qPopResult = q.Pop();
				if (qPopResult == null)
				{
					break;
				}
				var (constraintIndex, transforms) = qPopResult;
				var before = constraints[constraintIndex];
				var after = before;
				foreach (var t in transforms)
				{
					after = t(after);
				}
				if (!before.IsEquiavelentTo(after))
				{
					constraints = constraints.SetItem(constraintIndex, after);
					if (after.Pieces.Count() == 0)
					{
						return null;
					}
					if ((after.Pieces.Count() == 1) && (before.Pieces.Count() > 1))
					{
						var thePieceIndex = after.Pieces.First();
						for(var i = 0; i < 256; ++i)
						{
							if (i != constraintIndex)
							{
								q.Push(
									i,
									c => c.RemovePossiblePiece(thePieceIndex)
								);
							}
						}
					}
					if (before.PatternConstraints.Left.Count() != after.PatternConstraints.Left.Count())
					{
						var adjPositionIndex = TransformPositionIndex(
							constraintIndex,
							Positions.Left
						);
						if (adjPositionIndex != null)
						{
							var left = after.PatternConstraints.Left;
							q.Push(
								adjPositionIndex.Value,
								c => c.ConstrainRightPattern(left)
							);
						}
					}
					if (before.PatternConstraints.Top.Count() != after.PatternConstraints.Top.Count())
					{
						var adjPositionIndex = TransformPositionIndex(
							constraintIndex,
							Positions.Above
						);
						if (adjPositionIndex != null)
						{
							var top = after.PatternConstraints.Top;
							q.Push(
								adjPositionIndex.Value,
								c => c.ConstrainBottomPattern(top)
							);
						}
					}
					if (before.PatternConstraints.Right.Count() != after.PatternConstraints.Right.Count())
					{
						var adjPositionIndex = TransformPositionIndex(
							constraintIndex,
							Positions.Right
						);
						if (adjPositionIndex != null)
						{
							var right = after.PatternConstraints.Right;
							q.Push(
								adjPositionIndex.Value,
								c => c.ConstrainLeftPattern(right)
							);
						}
					}
					if (before.PatternConstraints.Bottom.Count() != after.PatternConstraints.Bottom.Count())
					{
						var adjPositionIndex = TransformPositionIndex(
							constraintIndex,
							Positions.Below
						);
						if (adjPositionIndex != null)
						{
							var bottom = after.PatternConstraints.Bottom;
							q.Push(
								adjPositionIndex.Value,
								c => c.ConstrainTopPattern(bottom)
							);
						}
					}
				}
			}
			return constraints;
		}

		public static ImmutableArray<SquareConstraint>? GenerateInitialPlacements(IReadOnlyList<ImmutableArray<int>> pieceSides)
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
			var q = new SquareConstraintTransformQueue();
			for (int placementIndex = 0; placementIndex < 256; ++placementIndex)
			{
				var position = Positions.PositionLookup[placementIndex];
				if (position.X == 0)
				{
					q.Push(placementIndex, c => c.SetLeftPattern(23));
				}
				if (position.Y == 0)
				{
					q.Push(placementIndex, c => c.SetTopPattern(23));
				}
				if (position.X == 15)
				{
					q.Push(placementIndex, c => c.SetRightPattern(23));
				}
				if (position.Y == 15)
				{
					q.Push(placementIndex, c => c.SetBottomPattern(23));
				}
			}
			return ProcessQueue(constraints, q);
		}

		public static ImmutableArray<SquareConstraint>? SetPlacement(
			this ImmutableArray<SquareConstraint> constraints,
			int positionIndex,
			Placement placement)
		{
			var q = new SquareConstraintTransformQueue();
			for (int i = 0; i < constraints.Length; ++i)
			{
				if (i == positionIndex)
				{
					q.Push(i, c => c.SetPlacement(placement));
				}
				else
				{
					q.Push(i, c => c.RemovePossiblePiece(placement.PieceIndex));
				}
			}
			return ProcessQueue(constraints, q);
		}
	}
}

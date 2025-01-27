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

		public static MultiPatternConstraints Intersect(
			this MultiPatternConstraints m1,
			MultiPatternConstraints m2
		) =>
			new MultiPatternConstraints
			{
				Left = m1.Left.Intersect(m2.Left),
				Right = m1.Right.Intersect(m2.Right),
				Top = m1.Top.Intersect(m2.Top),
				Bottom = m1.Bottom.Intersect(m2.Bottom)
			};
	}

	public record class SquareConstraint
	{
		public required ImmutableHashSet<int> Pieces { get; init; }

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
			foreach (var rotation in placement.Rotations)
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
			return this with
			{
				Pieces = new[] { placement.PieceIndex }.ToImmutableHashSet(),
				PatternConstraints = newPatternConstraints.Intersect(this.PatternConstraints)
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

		private(
			MultiPatternConstraints,
			ImmutableHashSet<int>
		) UpdatePatternConstraintsBasedOnPiecesRecursive(
			MultiPatternConstraints patternContraints,
			ImmutableHashSet<int> pieces
		)
		{
			var newConstraints = AdjustPatternConstraintsBasedOnAvailablePieces(
				pieces,
				patternContraints
			);
			if (newConstraints.IsEquivalentTo(patternContraints))
			{
				return (
					patternContraints, 
					FilterSetBasedOnPatterns(pieces, patternContraints)
				);
			}
			else
			{
				return UpdatePiecesBasedOnPatternConstraintsRecursive(
					newConstraints,
					pieces
				);
			}
		}

		private (
			MultiPatternConstraints,
			ImmutableHashSet<int>
		) UpdatePiecesBasedOnPatternConstraintsRecursive(
			MultiPatternConstraints patternContraints,
			ImmutableHashSet<int> pieces
		)
		{
			var newPieces = FilterSetBasedOnPatterns(pieces, patternContraints);
			if (newPieces.IsEquivalentTo(pieces))
			{
				return (
					AdjustPatternConstraintsBasedOnAvailablePieces(
						newPieces,
						patternContraints
					), 
					pieces
				);
			}
			else
			{
				return UpdatePatternConstraintsBasedOnPiecesRecursive(
					patternContraints,
					pieces
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
			(newConstraints, var newPieces) = UpdatePiecesBasedOnPatternConstraintsRecursive(
				newConstraints,
				this.Pieces
			);
			return this with
			{
				PatternConstraints = newConstraints,
				Pieces = newPieces
			};
		}

		public SquareConstraint RemovePossiblePiece(
			int pieceIndex
		)
		{
			if (Pieces.Contains(pieceIndex))
			{
				var newPieces = this.Pieces.Remove(pieceIndex);
				var newConstraints = AdjustPatternConstraintsBasedOnAvailablePieces(
					newPieces,
					this.PatternConstraints
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


		public delegate (MultiPatternConstraints, ImmutableHashSet<int>) TransformAction(MultiPatternConstraints mpc, ImmutableHashSet<int> pieces);

		public SquareConstraint Transform(IEnumerable<TransformAction> actions)
		{
			var newPatternConstraints = this.PatternConstraints;
			var newPieces = this.Pieces;
			foreach(var a in actions)
			{
				(newPatternConstraints, newPieces) = a(newPatternConstraints, newPieces);
			}
			if (!newPatternConstraints.IsEquivalentTo(this.PatternConstraints))
			{
				(newPatternConstraints, newPieces) = UpdatePiecesBasedOnPatternConstraintsRecursive(newPatternConstraints, newPieces);
			}
			else if (!newPieces.IsEquivalentTo(this.Pieces))
			{
				(newPatternConstraints, newPieces) = UpdatePatternConstraintsBasedOnPiecesRecursive(newPatternConstraints, newPieces);
			}
			return this with
			{
				PatternConstraints = newPatternConstraints,
				Pieces = newPieces
			};
		}

	}

	public static class SquareConstraintExtensions
	{
		public static ImmutableHashSet<int> AllPatterns = Enumerable.Range(0, 48).ToImmutableHashSet();
		public static ImmutableHashSet<int> NotEdgePatterns = AllPatterns.Remove(23);

		public static bool IsEquiavelentTo(
			this SquareConstraint c1,
			SquareConstraint c2
		) =>
			ReferenceEquals(c1, c2)
			|| (
				c1.Pieces.IsEquivalentTo(c2.Pieces)
				&& c1.PatternConstraints.IsEquivalentTo(c2.PatternConstraints)
			);

		private static int? TransformPositionIndex(Positioner positioner, int positionIndex, Func<Position, Position> t)
		{
			var position = positioner.PositionLookup[positionIndex];
			var adjacentPosition = t(position);
			if (positioner.ReversePositionLookup.TryGetValue(adjacentPosition, out var result))
			{
				return new int?(result);
			}
			else
			{
				return default;
			}
		}

		public static class Transforms
		{
			public static SquareConstraint.TransformAction SetPlacement(Placement p) =>
				(patterns, pieces) => (patterns, new[] { p.PieceIndex }.ToImmutableHashSet());

			public static SquareConstraint.TransformAction RemovePossiblePiece(int pieceIndex) =>
				(patterns, pieces) => (patterns, pieces.Remove(pieceIndex));

			public static SquareConstraint.TransformAction ModifyPatterns(Func<MultiPatternConstraints, MultiPatternConstraints> f) =>
				(patterns, pieces) => (f(patterns), pieces);

			public static SquareConstraint.TransformAction SetLeftPattern(int pattern) =>
				ModifyPatterns(
					p => p with { Left = new[] { pattern }.ToImmutableHashSet() }
				);

			public static SquareConstraint.TransformAction SetTopPattern(int pattern) =>
				ModifyPatterns(
					p => p with { Top = new[] { pattern }.ToImmutableHashSet() }
				);

			public static SquareConstraint.TransformAction SetRightPattern(int pattern) =>
				ModifyPatterns(
					p => p with { Right = new[] { pattern }.ToImmutableHashSet() }
				);

			public static SquareConstraint.TransformAction SetBottomPattern(int pattern) =>
				ModifyPatterns(
					p => p with { Bottom = new[] { pattern }.ToImmutableHashSet() }
				);

			public static SquareConstraint.TransformAction ConstrainRightPattern(
				ImmutableHashSet<int> patterns
			) =>
				ModifyPatterns(
					p => p with { Right = p.Right.Constrain(patterns) }
				);

			public static SquareConstraint.TransformAction ConstrainTopPattern(
				ImmutableHashSet<int> patterns
			) =>
				ModifyPatterns(
					p => p with { Top = p.Top.Constrain(patterns) }
				);


			public static SquareConstraint.TransformAction ConstrainBottomPattern(
				ImmutableHashSet<int> patterns
			) =>
				ModifyPatterns(
					p => p with { Bottom = p.Bottom.Constrain(patterns) }
				);

			public static SquareConstraint.TransformAction ConstrainLeftPattern(
				ImmutableHashSet<int> patterns
			) =>
				ModifyPatterns(
					p => p with { Left = p.Left.Constrain(patterns) }
				);
		}

		private static ImmutableArray<SquareConstraint>? ProcessQueue(
			ImmutableArray<SquareConstraint> constraints,
			SquareConstraintTransformQueue q,
			Positioner positioner
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
				var after = before.Transform(transforms);
				if (!before.IsEquiavelentTo(after))
				{
					constraints = constraints.SetItem(constraintIndex, after);
					if (after.Pieces.Count() == 0)
					{
						// The board is in an invalid state.
						// Abort whatever triggered this.
						return null;
					}
					if ((after.Pieces.Count() == 1) && (before.Pieces.Count() > 1))
					{
						var thePieceIndex = after.Pieces.First();
						for (var i = 0; i < constraints.Length; ++i)
						{
							if (i != constraintIndex)
							{
								q.Push(
									i,
									Transforms.RemovePossiblePiece(thePieceIndex)
								);
							}
						}
					}
					if (before.PatternConstraints.Left.Count() != after.PatternConstraints.Left.Count())
					{
						var adjPositionIndex = TransformPositionIndex(
							positioner,
							constraintIndex,
							Positions.Left
						);
						if (adjPositionIndex != null)
						{
							var left = after.PatternConstraints.Left;
							q.Push(
								adjPositionIndex.Value,
								Transforms.ConstrainRightPattern(left)
							);
						}
					}
					if (before.PatternConstraints.Top.Count() != after.PatternConstraints.Top.Count())
					{
						var adjPositionIndex = TransformPositionIndex(
							positioner,
							constraintIndex,
							Positions.Above
						);
						if (adjPositionIndex != null)
						{
							var top = after.PatternConstraints.Top;
							q.Push(
								adjPositionIndex.Value,
								Transforms.ConstrainBottomPattern(top)
							);
						}
					}
					if (before.PatternConstraints.Right.Count() != after.PatternConstraints.Right.Count())
					{
						var adjPositionIndex = TransformPositionIndex(
							positioner,
							constraintIndex,
							Positions.Right
						);
						if (adjPositionIndex != null)
						{
							var right = after.PatternConstraints.Right;
							q.Push(
								adjPositionIndex.Value,
								Transforms.ConstrainLeftPattern(right)
							);
						}
					}
					if (before.PatternConstraints.Bottom.Count() != after.PatternConstraints.Bottom.Count())
					{
						var adjPositionIndex = TransformPositionIndex(
							positioner,
							constraintIndex,
							Positions.Below
						);
						if (adjPositionIndex != null)
						{
							var bottom = after.PatternConstraints.Bottom;
							q.Push(
								adjPositionIndex.Value,
								Transforms.ConstrainTopPattern(bottom)
							);
						}
					}
				}
			}
			return constraints;
		}


		public static ImmutableArray<SquareConstraint>? GenerateInitialPlacements(IReadOnlyList<ImmutableArray<int>> pieceSides)
		{
			var constraintsArray = new SquareConstraint[pieceSides.Count];

			ImmutableHashSet<int> AllPieces = Enumerable.Range(0, pieceSides.Count)
				.ToImmutableHashSet();

			var initialConstraint = new SquareConstraint
			{
				Pieces = AllPieces,
				PiecePatternLookup = pieceSides
			};
			for (int placementIndex = 0; placementIndex < constraintsArray.Length; ++placementIndex)
			{
				constraintsArray[placementIndex] = initialConstraint;
			}
			var positioner = Positioner.Generate(pieceSides.Count);

			var constraints = constraintsArray.ToImmutableArray();
			var q = new SquareConstraintTransformQueue();
			int sideLength = (int)(Math.Round(Math.Sqrt(pieceSides.Count)));
			for (int placementIndex = 0; placementIndex < pieceSides.Count; ++placementIndex)
			{
				var position = positioner.PositionLookup[placementIndex];
				if (position.X == 0)
				{
					q.Push(placementIndex, Transforms.SetLeftPattern(23));
				}
				else
				{
					q.Push(placementIndex, Transforms.ConstrainLeftPattern(NotEdgePatterns));
				}
				if (position.Y == 0)
				{
					q.Push(placementIndex, Transforms.SetTopPattern(23));
				}
				else
				{
					q.Push(placementIndex, Transforms.ConstrainTopPattern(NotEdgePatterns));
				}
				if (position.X == sideLength-1)
				{
					q.Push(placementIndex, Transforms.SetRightPattern(23));
				}
				else
				{
					q.Push(placementIndex, Transforms.ConstrainRightPattern(NotEdgePatterns));
				}
				if (position.Y == sideLength-1)
				{
					q.Push(placementIndex, Transforms.SetBottomPattern(23));
				}
				else
				{
					q.Push(placementIndex, Transforms.ConstrainBottomPattern(NotEdgePatterns));
				}
			}
			return ProcessQueue(constraints, q, positioner);
		}


		public static ImmutableArray<SquareConstraint>? SetPlacement(
			this ImmutableArray<SquareConstraint> constraints,
			int positionIndex,
			Placement placement,
			Positioner positioner
			)
		{
			var q = new SquareConstraintTransformQueue();
			for (int i = 0; i < constraints.Length; ++i)
			{
				if (i == positionIndex)
				{
					q.Push(i, Transforms.SetPlacement(placement));
				}
				else
				{
					q.Push(i, Transforms.RemovePossiblePiece(placement.PieceIndex));
				}
			}
			return ProcessQueue(constraints, q, positioner);
		}
	}
}
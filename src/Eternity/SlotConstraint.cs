
namespace Eternity
{
	using System.Collections.Immutable;
	using System.Data;

	public static class ImmutableHashSetExtensions
	{
		public static ImmutableBitArray Constrain(this ImmutableBitArray s1, ImmutableBitArray c) =>
			s1.Intersect(c);
	}

	public record MultiPatternConstraints
	{
		public required ImmutableBitArray Left;
		public required ImmutableBitArray Right;
		public required ImmutableBitArray Top;
		public required ImmutableBitArray Bottom;

		public readonly static MultiPatternConstraints Never = new()
		{
			Left = ImmutableBitArray.Empty,
			Bottom = ImmutableBitArray.Empty,
			Right = ImmutableBitArray.Empty,
			Top = ImmutableBitArray.Empty
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

	public record class SlotConstraint
	{
		public required ImmutableBitArray Pieces { get; init; }

		public MultiPatternConstraints PatternConstraints = new()
		{
			Bottom = SquareConstraintExtensions.AllPatterns,
			Top = SquareConstraintExtensions.AllPatterns,
			Left = SquareConstraintExtensions.AllPatterns,
			Right = SquareConstraintExtensions.AllPatterns
		};

		public required IReadOnlyList<ImmutableArray<int>> PiecePatternLookup { get; init; }
		public SlotConstraint()
		{
		}

		public SlotConstraint SetPlacement(
			Placement placement
		)
		{
			var newPatternConstraints = new MultiPatternConstraints
			{
				Left = ImmutableBitArray.Empty,
				Bottom = ImmutableBitArray.Empty,
				Right = ImmutableBitArray.Empty,
				Top = ImmutableBitArray.Empty,
			};
			var patterns = this.PiecePatternLookup[placement.PieceIndex];
			var rotated = new int[4];
			foreach (var rotation in placement.Rotations)
			{
				RotationExtensions.Rotate(patterns, rotation, rotated);
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
				Pieces = ImmutableBitArray.SingleValue(placement.PieceIndex),
				PatternConstraints = newPatternConstraints.Intersect(this.PatternConstraints)
			};
		}

		// THIS IS THE METHOD THAT IT IS ALWAYS IN
		private static MultiPatternConstraints AdjustPatternConstraintsBasedOnAvailablePieces(
			IEnumerable<int> availablePieces,
			MultiPatternConstraints currentConstraints,
			IReadOnlyList<ImmutableArray<int>> piecePatternLookup
		)
		{
			var top = ImmutableBitArray.Empty;
			var bottom = ImmutableBitArray.Empty;
			var left = ImmutableBitArray.Empty;
			var right = ImmutableBitArray.Empty;
			int[] rotatedPattern = new int[4];
			foreach (var pieceIndex in availablePieces)
			{
				var piecePattern = piecePatternLookup[pieceIndex];
				foreach (var rotation in RotationExtensions.AllRotations)
				{
					RotationExtensions.Rotate(piecePattern, rotation, rotatedPattern);
					if (
						currentConstraints.Top.Contains(rotatedPattern[EdgeIndexes.Top])
						&& currentConstraints.Left.Contains(rotatedPattern[EdgeIndexes.Left])
						&& currentConstraints.Bottom.Contains(rotatedPattern[EdgeIndexes.Bottom])
						&& currentConstraints.Right.Contains(rotatedPattern[EdgeIndexes.Right])
					)
					{
						top = top.Add(rotatedPattern[EdgeIndexes.Top]);
						bottom = bottom.Add(rotatedPattern[EdgeIndexes.Bottom]);
						left = left.Add(rotatedPattern[EdgeIndexes.Left]);
						right = right.Add(rotatedPattern[EdgeIndexes.Right]);
						if (
							(top.Count == currentConstraints.Top.Count)
							&&
							(left.Count == currentConstraints.Left.Count)
							&&
							(right.Count == currentConstraints.Right.Count)
							&&
							(bottom.Count == currentConstraints.Bottom.Count)
						)
						{
							return currentConstraints;
						}
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

		private ImmutableBitArray FilterSetBasedOnPatterns(
			ImmutableBitArray hashSet,
			MultiPatternConstraints c
		)
		{
			var rv = hashSet;
			int[] rotatedPattern = new int[4];
			foreach (var pieceIndex in hashSet)
			{
				var piecePattern = this.PiecePatternLookup[pieceIndex];
				bool approved = false;
				foreach (var rotation in RotationExtensions.AllRotations)
				{
					RotationExtensions.Rotate(piecePattern, rotation, rotatedPattern);
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
			ImmutableBitArray
		) UpdatePatternConstraintsBasedOnPiecesRecursive(
			MultiPatternConstraints patternContraints,
			ImmutableBitArray pieces
		)
		{
			var newConstraints = AdjustPatternConstraintsBasedOnAvailablePieces(
				pieces,
				patternContraints,
				this.PiecePatternLookup
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
			ImmutableBitArray
		) UpdatePiecesBasedOnPatternConstraintsRecursive(
			MultiPatternConstraints patternContraints,
			ImmutableBitArray pieces
		)
		{
			var newPieces = FilterSetBasedOnPatterns(pieces, patternContraints);
			if (newPieces.IsEquivalentTo(pieces))
			{
				return (
					AdjustPatternConstraintsBasedOnAvailablePieces(
						newPieces,
						patternContraints,
						this.PiecePatternLookup
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

		private SlotConstraint ModifyPatternConstraints(
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

		public SlotConstraint SetTopPattern(
			int pattern
		) => ModifyPatternConstraints(
			mp => mp with { Top = ImmutableBitArray.SingleValue(pattern) }
		);

		public SlotConstraint SetLeftPattern(
			int pattern
		) => ModifyPatternConstraints(
			mp => mp with { Left = ImmutableBitArray.SingleValue(pattern) }
		);

		public SlotConstraint ConstrainLeftPattern(
			ImmutableBitArray patterns
		) => ModifyPatternConstraints(
			mp =>
				mp with
				{
					Left = ImmutableHashSetExtensions.Constrain(mp.Left, patterns)
				}
		);

		public SlotConstraint ConstrainTopPattern(
			ImmutableBitArray patterns
		) => ModifyPatternConstraints(
			mp =>
				mp with
				{
					Top = ImmutableHashSetExtensions.Constrain(mp.Top, patterns)
				}
		);

		public SlotConstraint ConstrainBottomPattern(
			ImmutableBitArray patterns
		) => ModifyPatternConstraints(
			mp =>
				mp with
				{
					Bottom = ImmutableHashSetExtensions.Constrain(mp.Bottom, patterns)
				}
		);

		public SlotConstraint ConstrainRightPattern(
			ImmutableBitArray patterns
		) => ModifyPatternConstraints(
			mp =>
				mp with
				{
					Right = ImmutableHashSetExtensions.Constrain(mp.Right, patterns)
				}
		);

		public SlotConstraint SetRightPattern(
			int pattern
		) => ModifyPatternConstraints(
			mp => mp with { Right = ImmutableBitArray.SingleValue(pattern) }
		);

		public SlotConstraint SetBottomPattern(
			int pattern
		) => ModifyPatternConstraints(
			mp => mp with { Bottom = ImmutableBitArray.SingleValue(pattern) }
		);


		public delegate (MultiPatternConstraints, ImmutableBitArray) TransformAction(MultiPatternConstraints mpc, ImmutableBitArray pieces);

		public SlotConstraint Transform(IEnumerable<TransformAction> actions)
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
		public static ImmutableBitArray AllPatterns = ImmutableBitArray.AllPieces(49);
		public static ImmutableBitArray NotEdgePatterns = AllPatterns.Remove(23);

		public static bool IsEquivalentTo(
			this SlotConstraint c1,
			SlotConstraint c2
		) =>
			ReferenceEquals(c1, c2)
			|| (
				c1.Pieces.IsEquivalentTo(c2.Pieces)
				&& c1.PatternConstraints.IsEquivalentTo(c2.PatternConstraints)
			);

		private static Position? TryTransformPosition(
			Dimensions dimensions,
			Position position,
			Func<Position, Position> t
		)
		{
			var newPosition = t(position);
			return dimensions.Contains(newPosition)
				? newPosition
				: null;
		}

		public static class Transforms
		{
			public static SlotConstraint.TransformAction SetPlacement(Placement p) =>
				(patterns, pieces) => (patterns, ImmutableBitArray.SingleValue(p.PieceIndex));

			public static SlotConstraint.TransformAction RemovePossiblePiece(int pieceIndex) =>
				(patterns, pieces) => (patterns, pieces.Remove(pieceIndex));

			public static SlotConstraint.TransformAction ModifyPatterns(Func<MultiPatternConstraints, MultiPatternConstraints> f) =>
				(patterns, pieces) => (f(patterns), pieces);

			public static SlotConstraint.TransformAction SetLeftPattern(int pattern) =>
				ModifyPatterns(
					p => p with { Left = ImmutableBitArray.SingleValue(pattern) }
				);

			public static SlotConstraint.TransformAction SetTopPattern(int pattern) =>
				ModifyPatterns(
					p => p with { Top = ImmutableBitArray.SingleValue(pattern) }
				);

			public static SlotConstraint.TransformAction SetRightPattern(int pattern) =>
				ModifyPatterns(
					p => p with { Right = ImmutableBitArray.SingleValue(pattern) }
				);

			public static SlotConstraint.TransformAction SetBottomPattern(int pattern) =>
				ModifyPatterns(
					p => p with { Bottom = ImmutableBitArray.SingleValue(pattern) }
				);

			public static SlotConstraint.TransformAction ConstrainRightPattern(
				ImmutableBitArray patterns
			) =>
				ModifyPatterns(
					p => p with { Right = p.Right.Constrain(patterns) }
				);

			public static SlotConstraint.TransformAction ConstrainTopPattern(
				ImmutableBitArray patterns
			) =>
				ModifyPatterns(
					p => p with { Top = p.Top.Constrain(patterns) }
				);


			public static SlotConstraint.TransformAction ConstrainBottomPattern(
				ImmutableBitArray patterns
			) =>
				ModifyPatterns(
					p => p with { Bottom = p.Bottom.Constrain(patterns) }
				);

			public static SlotConstraint.TransformAction ConstrainLeftPattern(
				ImmutableBitArray patterns
			) =>
				ModifyPatterns(
					p => p with { Left = p.Left.Constrain(patterns) }
				);
		}

		private static ImmutableArray<SlotConstraint>? ProcessQueue(
			ImmutableArray<SlotConstraint> origionalConstraints,
			SlotConstraintTransformQueue q,
			Dimensions dimensions
		)
		{
			SlotConstraint[] constraints = origionalConstraints.ToArray();
			bool anyChanges = false;
			while (q.HasItems)
			{
				var newItemsWithTwo = new List<int>();
				while (q.HasItems)
				{
					var qPopResult = q.Pop();
					if (qPopResult == null)
					{
						break;
					}
					var (position, transforms) = qPopResult;
					var constraintIndex = dimensions.PositionToIndex(position);
					var before = constraints[constraintIndex];
					var after = before.Transform(transforms);
					if (!before.IsEquivalentTo(after))
					{
						anyChanges = true;
						constraints[constraintIndex] = after;
						if (after.Pieces.Count() == 0)
						{
							// The board is in an invalid state.
							// Abort whatever triggered this.
							return null;
						}
						else if ((after.Pieces.Count() == 1) && (before.Pieces.Count() > 1))
						{
							var thePieceIndex = after.Pieces.First();
							var removePiece = Transforms.RemovePossiblePiece(thePieceIndex);
							for (var i = 0; i < constraints.Length; ++i)
							{
								if ((i != constraintIndex) && constraints[i].Pieces.Contains(thePieceIndex))
								{
									q.Push(
										dimensions.IndexToPosition(i),
										removePiece
									);
								}
							}
						}
						else if ((after.Pieces.Count() == 2) && (before.Pieces.Count() > 2))
						{
							newItemsWithTwo.Add(constraintIndex);
						}
						if (before.PatternConstraints.Left.Count() != after.PatternConstraints.Left.Count())
						{
							var adjPosition = TryTransformPosition(
								dimensions,
								position,
								Positions.Left
							);
							if (adjPosition != null)
							{
								var left = after.PatternConstraints.Left;
								q.Push(
									adjPosition,
									Transforms.ConstrainRightPattern(left)
								);
							}
						}
						if (before.PatternConstraints.Top.Count() != after.PatternConstraints.Top.Count())
						{
							var adjPosition = TryTransformPosition(
								dimensions,
								position,
								Positions.Above
							);
							if (adjPosition != null)
							{
								var top = after.PatternConstraints.Top;
								q.Push(
									adjPosition,
									Transforms.ConstrainBottomPattern(top)
								);
							}
						}
						if (before.PatternConstraints.Right.Count() != after.PatternConstraints.Right.Count())
						{
							var adjPosition = TryTransformPosition(
								dimensions,
								position,
								Positions.Right
							);
							if (adjPosition != null)
							{
								var right = after.PatternConstraints.Right;
								q.Push(
									adjPosition,
									Transforms.ConstrainLeftPattern(right)
								);
							}
						}
						if (before.PatternConstraints.Bottom.Count() != after.PatternConstraints.Bottom.Count())
						{
							var adjPosition = TryTransformPosition(
								dimensions,
								position,
								Positions.Below
							);
							if (adjPosition != null)
							{
								var bottom = after.PatternConstraints.Bottom;
								q.Push(
									adjPosition,
									Transforms.ConstrainTopPattern(bottom)
								);
							}
						}
					}
				}
				if (newItemsWithTwo.Any())
				{
					foreach(var newItemIndex in newItemsWithTwo)
					{
						var newItemConstraint = constraints[newItemIndex];
						if (newItemConstraint.Pieces.Count() != 2)
						{
							continue;
						}
						var matchingIndexes = new List<int>();
						for(int i = 0; i < constraints.Length; ++i)
						{
							if (i == newItemIndex)
							{
								continue;
							}
							var trialConstraint = constraints[i];
							if (trialConstraint.Pieces.IsEquivalentTo(newItemConstraint.Pieces))
							{
								matchingIndexes.Add(i);
							}
						}
						if (matchingIndexes.Count > 1)
						{
							return null;
						}
						else if (matchingIndexes.Count == 1)
						{
							for(int i = 0; i < constraints.Length; ++i)
							{
								if ((i != newItemIndex) && (i != matchingIndexes[0]))
								{
									foreach (var p in newItemConstraint.Pieces)
									{
										if (constraints[i].Pieces.Contains(p))
										{
											q.Push(
												dimensions.IndexToPosition(i),
												Transforms.RemovePossiblePiece(p)
											);
										}
									}
								}
							}
						}
					}
				}
			}
			return anyChanges ? constraints.ToImmutableArray() : origionalConstraints;
		}

		public static ImmutableArray<SlotConstraint>? GenerateInitialPlacements(IReadOnlyList<ImmutableArray<int>> pieceSides)
		{
			var constraintsArray = new SlotConstraint[pieceSides.Count];

			ImmutableBitArray AllPieces = ImmutableBitArray.AllPieces(pieceSides.Count);

			var initialConstraint = new SlotConstraint
			{
				Pieces = AllPieces,
				PiecePatternLookup = pieceSides
			};
			for (int placementIndex = 0; placementIndex < constraintsArray.Length; ++placementIndex)
			{
				constraintsArray[placementIndex] = initialConstraint;
			}
			var squareRoot = (int)Math.Round(Math.Sqrt(pieceSides.Count));
			var dimensions = new Dimensions(squareRoot, squareRoot);

			var constraints = constraintsArray.ToImmutableArray();
			var q = new SlotConstraintTransformQueue();
			int sideLength = (int)(Math.Round(Math.Sqrt(pieceSides.Count)));
			for (int placementIndex = 0; placementIndex < pieceSides.Count; ++placementIndex)
			{
				var position = dimensions.IndexToPosition(placementIndex);
				if (position.X == 0)
				{
					q.Push(position, 
						Transforms.SetLeftPattern(23)
					);
				}
				else
				{
					q.Push(position, Transforms.ConstrainLeftPattern(NotEdgePatterns));
				}
				if (position.Y == 0)
				{
					q.Push(position, Transforms.SetTopPattern(23));
				}
				else
				{
					q.Push(position, Transforms.ConstrainTopPattern(NotEdgePatterns));
				}
				if (position.X == sideLength-1)
				{
					q.Push(position, Transforms.SetRightPattern(23));
				}
				else
				{
					q.Push(position, Transforms.ConstrainRightPattern(NotEdgePatterns));
				}
				if (position.Y == sideLength-1)
				{
					q.Push(position, Transforms.SetBottomPattern(23));
				}
				else
				{
					q.Push(position, Transforms.ConstrainBottomPattern(NotEdgePatterns));
				}
			}

			
			var nullableResult = ProcessQueue(constraints, q, dimensions);
			if (nullableResult == null)
			{
				return null;
			}
			// Find the first corner piece and put it in the top left corner
			// It never moves!
			int firstCornerPieceIndex = -1;
			for(int i = 0; i < pieceSides.Count; ++i)
			{
				if (pieceSides[i].Where(ps => ps == 23).Count() == 2)
				{
					firstCornerPieceIndex = i;
					break;
				}
			}
			if (firstCornerPieceIndex == -1)
			{
				return null;
			}
			return nullableResult?.SetPlacement(
				new Placement(new Position(0, 0), firstCornerPieceIndex, []),
				dimensions
			);
		}


		public static ImmutableArray<SlotConstraint>? SetPlacement(
			this ImmutableArray<SlotConstraint> constraints,
			Placement placement,
			Dimensions dimensions
			)
		{
			var q = new SlotConstraintTransformQueue();
			int positionIndex = dimensions.PositionToIndex(placement.Position);
			var removePiece = Transforms.RemovePossiblePiece(placement.PieceIndex);
			for (int i = 0; i < constraints.Length; ++i)
			{
				if (i == positionIndex)
				{
					q.Push(
						dimensions.IndexToPosition(i),
						Transforms.SetPlacement(placement)
					);
				}
				else
				{
					q.Push(
						dimensions.IndexToPosition(i),
						removePiece
					);
				}
			}
			return ProcessQueue(constraints, q, dimensions);
		}
	}
}
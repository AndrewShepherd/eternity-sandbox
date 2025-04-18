namespace Eternity
{
	using System.Collections.Immutable;
	using System.Runtime.CompilerServices;

	public static class PuzzleSolver
	{
		private static IEnumerable<Position> GetAdjacentPositions(Position p)
		{
			if (p.X > 0)
			{
				yield return new Position(p.X - 1, p.Y);
			}
			if (p.Y > 0)
			{
				yield return new Position(p.X, p.Y - 1);
			}
			if (p.X < 15)
			{
				yield return new Position(p.X + 1, p.Y);
			}
			if (p.Y < 15)
			{
				yield return new Position(p.X, p.Y + 1);
			}
		}


		private static IEnumerable<int> GetAdjacentPlacementIndexes(
			int placementIndex,
			Positioner positioner
		)
		{
			var thisPosition = positioner.PositionLookup[placementIndex];
			foreach(var position in GetAdjacentPositions(thisPosition))
			{
				if (positioner.ReversePositionLookup.TryGetValue(position, out var index))
				{
					yield return index;
				}
			}
		}

		private static IEnumerable<int?> GetAdjacentSideColor(
			Position position,
			Placements existingPlacements,
			int edgeIndex
		)
		{
			if (existingPlacements.Positioner.ReversePositionLookup.TryGetValue(position, out var placementIndex))
			{
				var adjacentPlacement = existingPlacements.Values[placementIndex];
				if (adjacentPlacement != null)
				{
					var adjacentColors = existingPlacements.PieceSides[adjacentPlacement.PieceIndex];
					return adjacentPlacement.Rotations.Select(
						r => RotationExtensions.Rotate(adjacentColors, r)
					).Select(topColorsRotated => topColorsRotated[edgeIndex])
					.Select(n => (int?)n);
				}
			}
			return [default];
		}

		private static IEnumerable<EdgeRequirements> GetEdgeRequirements(
			Placements existingPlacements,
			int targetPositionIndex
		)
		{
			Func<int, int, int, IEnumerable<int?>> getAdjacentSideColor = (x, y, edgeIndex) => GetAdjacentSideColor(
				new Position(x, y),
				existingPlacements,
				edgeIndex
			);
			var target = existingPlacements.Positioner.PositionLookup[targetPositionIndex];

			IEnumerable<int?> edgeColor = [23];

			var topColors = (target.Y == 0)
				? edgeColor
				: getAdjacentSideColor(target.X, target.Y - 1, EdgeIndexes.Bottom);
			var leftColors = (target.X == 0)
				? edgeColor
				: getAdjacentSideColor(target.X - 1, target.Y, EdgeIndexes.Right);
			var rightColors = (target.X == 15)
				? edgeColor
				: getAdjacentSideColor(target.X + 1, target.Y, EdgeIndexes.Left);
			var bottomColors = (target.Y == 15)
				? edgeColor
				: getAdjacentSideColor(target.X, target.Y + 1, EdgeIndexes.Top);

			IEnumerable<EdgeRequirements> edgeRequirements =
				from leftColor in leftColors
				from topColor in topColors
				from rightColor in rightColors
				from bottomColor in bottomColors
				select new EdgeRequirements(leftColor, topColor, rightColor, bottomColor);
			return edgeRequirements;
		}



		private static Rotation[] AllRotations = [
			Rotation.None,
			Rotation.Ninety,
			Rotation.OneEighty,
			Rotation.TwoSeventy
		];

		private static List<Rotation> GetPossibleRotations(
			Position position,
			int pieceIndex,
			Placements listPlacements
		)
		{
			var pc = listPlacements.Constraints.At(position).PatternConstraints;
			var patterns = listPlacements.PieceSides[pieceIndex];
			List<Rotation> result = new List<Rotation>();
			foreach(var rotation in RotationExtensions.AllRotations)
			{
				var rp = RotationExtensions.Rotate(patterns, rotation);
				if (
					pc.Left.Contains(rp[EdgeIndexes.Left])
					&& pc.Top.Contains(rp[EdgeIndexes.Top])
					&& pc.Right.Contains(rp[EdgeIndexes.Right])
					&& pc.Bottom.Contains(rp[EdgeIndexes.Bottom])
				)
				{
					result.Add(rotation);
				}
			}
			return result;
		}

		public static Placements? TryAddPiece(
			Placements listPlacements,
			int positionIndex,
			int pieceIndex
			)
		{
			Position position = listPlacements.Positioner.PositionLookup[positionIndex];
			var pieceIndexAlredyThere = listPlacements.Values[positionIndex]?.PieceIndex;
			if (pieceIndexAlredyThere != null)
			{
				if (pieceIndexAlredyThere == pieceIndex)
				{
					return listPlacements;
				}
				else
				{
					return null;
				}
			}
			if (!listPlacements.Constraints.At(position).Pieces.Contains(pieceIndex))
			{
				return null;
			}
			if (listPlacements.ContainsPieceIndex(pieceIndex))
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


			var attempt = listPlacements.SetItem(
				positionIndex,new Placement(pieceIndex, rotations.ToArray()));
			if (attempt == null)
			{
				return null;
			}
			listPlacements = attempt;
			// There may be existing placements which had multiple rotations
			// as a result of placing this piece they may no longer have multiple
			// rotations
			var adjacentPlacementIndexes = PuzzleSolver.GetAdjacentPlacementIndexes(
				positionIndex,
				listPlacements.Positioner
			)
				.Where(
					pi => listPlacements.Values[pi] != null
					&& listPlacements.Values[pi]!.Rotations.Length > 1
				).ToArray();
			if (adjacentPlacementIndexes.Length > 0)
			{
				foreach (var adjacentPlacementIndex in adjacentPlacementIndexes)
				{
					var thisPlacement = listPlacements.Values[adjacentPlacementIndex];
					if (thisPlacement == null)
					{
						throw new Exception("This cannot have happend");
					}
					// This test isn't necessary anymore
					// because of the constraint checking
					// I added afterwards
					var newRotations = PuzzleSolver.GetPossibleRotations(
						listPlacements.Positioner.PositionLookup[adjacentPlacementIndex],
						thisPlacement.PieceIndex,
						listPlacements
					);
					if (newRotations.Count == 0)
					{
						throw new Exception("After placing a piece, an existing piece was in an illegal state");
					}
					if (newRotations.Count < thisPlacement.Rotations.Length)
					{
						var thisAttempt = listPlacements.SetItem(
							adjacentPlacementIndex,
							new(
								thisPlacement.PieceIndex,
								newRotations.ToArray()
							)
						);
						if (thisAttempt == null)
						{
							return null;
						}
						listPlacements = thisAttempt;
					}
				}
			}

			List<int> positionsWithOneConstraint = [];
			for (
				int constraintIndex = 0;
				constraintIndex < listPlacements.Dimensions.Width * listPlacements.Dimensions.Height;
				++constraintIndex
			)
			{
				if (listPlacements.Values[constraintIndex] != null)
				{
					continue;
				}
				var constraintPosition = listPlacements.Positioner.PositionLookup[constraintIndex];
				var constraint = listPlacements.Constraints.At(constraintPosition);
				if (constraint.Pieces.Count() == 0)
				{
					return null;
				}
				if (constraint.Pieces.Count() == 1)
				{
					positionsWithOneConstraint.Add(constraintIndex);
				}
			}
			if (positionsWithOneConstraint.Any())
			{
				var positionsAndPieces = positionsWithOneConstraint.Select(
					i => new
					{
						PositionIndex = i,
						PieceIndex = listPlacements.Constraints.At(listPlacements.Positioner.PositionLookup[i]).Pieces.First()
					}
				).ToList();
				if (positionsAndPieces.Select(p => p.PieceIndex).Distinct().Count() < positionsAndPieces.Count())
				{
					// Cannot put the same piece in multiple positions
					return null;
				}
				var p = positionsAndPieces.First();
				return TryAddPiece(
					listPlacements,
					p.PositionIndex,
					p.PieceIndex
				);
			}
			return listPlacements;
		}

	}
}

namespace Eternity
{
	using System.Collections.Immutable;

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

		private static Rotation[] GetPossibleRotations(
			int positionIndex,
			int pieceIndex,
			Placements listPlacements
		)
		{
			;
			var edgeRequirements = GetEdgeRequirements(
				listPlacements,
				positionIndex
			);
			return edgeRequirements.SelectMany(
				er => GetRotations(listPlacements.PieceSides[pieceIndex], er)
			).ToArray();
		}

		private static IEnumerable<Rotation> GetRotations(
			ImmutableArray<int> sides,
			EdgeRequirements edgeRequirements
		)
		{
			foreach (var rotation in AllRotations)
			{
				var rotatedSides = RotationExtensions.Rotate(sides, rotation);
				EdgeRequirements thisRequirements = new EdgeRequirements(
					rotatedSides[EdgeIndexes.Left],
					rotatedSides[EdgeIndexes.Top],
					rotatedSides[EdgeIndexes.Right],
					rotatedSides[EdgeIndexes.Bottom]
				);
				if (thisRequirements.CanMatch(edgeRequirements))
				{
					yield return rotation;
				}
			}
		}

		public static Placements? TryAddPiece(
			Placements listPlacements,
			int positionIndex,
			int pieceIndex
			)
		{

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
			if (!listPlacements.Constraints[positionIndex].Pieces.Contains(pieceIndex))
			{
				return null;
			}
			if (listPlacements.ContainsPieceIndex(pieceIndex))
			{
				return null;
			}

			var rotations = PuzzleSolver.GetPossibleRotations(
				positionIndex,
				pieceIndex,
				listPlacements
			);

			if (rotations.Length == 0)
			{
				return null;
			}


			var attempt = listPlacements.SetItem(positionIndex, new Placement(pieceIndex, rotations));
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
					var newRotations = PuzzleSolver.GetPossibleRotations(
						adjacentPlacementIndex,
						thisPlacement.PieceIndex,
						listPlacements
					);
					if (newRotations.Length == 0)
					{
						throw new Exception("After placing a piece, an existing piece was in an illegal state");
					}
					if (newRotations.Length < thisPlacement.Rotations.Length)
					{
						var thisAttempt = listPlacements.SetItem(
							adjacentPlacementIndex,
							new(
								thisPlacement.PieceIndex,
								newRotations
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
				constraintIndex < listPlacements.Constraints.Count;
				++constraintIndex
			)
			{
				if (listPlacements.Values[constraintIndex] != null)
				{
					continue;
				}
				var constraint = listPlacements.Constraints[constraintIndex];
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
						PieceIndex = listPlacements.Constraints[i].Pieces.First()
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

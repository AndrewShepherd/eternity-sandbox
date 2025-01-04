using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternity.WpfApp
{

	static class PuzzleSolver
    {
		private static class EdgeIndexes
		{
			public const int Top = 0;
			public const int Right = 1;
			public const int Bottom = 2;
			public const int Left = 3;
		}

		private static ImmutableArray<int> Rotate(ImmutableArray<int> edges, Rotation rotation)
		{
			var result = new int[4];
			for (int i = 0; i < edges.Length; i++)
			{
				result[(i + (int)rotation) % 4] = edges[i];
			}
			return result.ToImmutableArray();
		}

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

		private static IEnumerable<int> GetAdjacentPlacementIndexes(PuzzleEnvironment puzzleEnvironment, int placementIndex)
		{
			var thisPosition = Positions.PositionLookup[placementIndex];
			return GetAdjacentPositions(thisPosition)
				.Select(p => Positions.ReversePositionLookup[p]);
		}

		private static IEnumerable<int?> GetAdjacentSideColor(
			PuzzleEnvironment puzzleEnvironment,
			Position position,
			Placements existingPlacements,
			int edgeIndex
		)
		{
			if (Positions.ReversePositionLookup.TryGetValue(position, out var placementIndex))
			{
				var adjacentPlacement = existingPlacements.Values[placementIndex];
				if (adjacentPlacement != null)
				{
					var adjacentColors = puzzleEnvironment.PieceSides[adjacentPlacement.PieceIndex];
					return adjacentPlacement.Rotations.Select(
						r => Rotate(adjacentColors, r)
					).Select(topColorsRotated => topColorsRotated[edgeIndex])
					.Select(n => (int?)n);
				}
			}
			return [default];
		}

		private static IEnumerable<EdgeRequirements> GetEdgeRequirements(
			PuzzleEnvironment puzzleEnvironment,
			Placements existingPlacements,
			int targetPositionIndex
		)
		{
			Func<int, int, int, IEnumerable<int?>> getAdjacentSideColor = (x, y, edgeIndex) => GetAdjacentSideColor(
				puzzleEnvironment,
				new Position(x, y),
				existingPlacements,
				edgeIndex
			);
			var target = Positions.PositionLookup[targetPositionIndex];

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

		private static bool CanMatch(int? n1, int? n2) =>
			(n1, n2) switch
			{
				(null, _) => true,
				(_, null) => true,
				_ => n1 == n2
			};

		private static bool CanMatch(EdgeRequirements r1, EdgeRequirements r2) =>
			CanMatch(r1.left, r2.left)
			&& CanMatch(r1.right, r2.right)
			&& CanMatch(r1.top, r2.top)
			&& CanMatch(r1.bottom, r2.bottom);

		private static Rotation[] AllRotations = [
			Rotation.None,
			Rotation.Ninety,
			Rotation.OneEighty,
			Rotation.TwoSeventy
		];

		private static Rotation[] GetPossibleRotations(
			PuzzleEnvironment puzzleEnvironment,
			int positionIndex,
			int pieceIndex,
			Placements listPlacements
		)
		{
			var sides = puzzleEnvironment.PieceSides[pieceIndex];
			var edgeRequirements = GetEdgeRequirements(
				puzzleEnvironment,
				listPlacements,
				positionIndex
			);
			return edgeRequirements.SelectMany(
				er => GetRotations(sides, er)
			).ToArray();
		}

		private static IEnumerable<Rotation> GetRotations(
			ImmutableArray<int> sides,
			EdgeRequirements edgeRequirements
		)
		{
			foreach(var rotation in AllRotations)
			{
				var rotatedSides = Rotate(sides, rotation);
				EdgeRequirements thisRequirements = new EdgeRequirements(
					rotatedSides[EdgeIndexes.Left],
					rotatedSides[EdgeIndexes.Top],
					rotatedSides[EdgeIndexes.Right],
					rotatedSides[EdgeIndexes.Bottom]
				);
				if (CanMatch(thisRequirements, edgeRequirements))
				{
					yield return rotation;
				}
			}
		}

		public static Placements? TryAddPiece(
			PuzzleEnvironment puzzleEnvironment,
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
			if (listPlacements.ContainsPieceIndex(pieceIndex))
			{
				return null;
			}

			var rotations = PuzzleSolver.GetPossibleRotations(
				puzzleEnvironment,
				positionIndex,
				pieceIndex,
				listPlacements
			);

			if (rotations.Length == 0)
			{
				return null;
			}


			listPlacements = listPlacements.SetItem(positionIndex, new Placement(pieceIndex, rotations));

			// There may be existing placements which had multiple rotations
			// as a result of placing this piece they may no longer have multiple
			// rotations
			var adjacentPlacementIndexes = PuzzleSolver.GetAdjacentPlacementIndexes(puzzleEnvironment, positionIndex)
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
						puzzleEnvironment,
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
						listPlacements = listPlacements.SetItem(
							adjacentPlacementIndex,
							new(
								thisPlacement.PieceIndex,
								newRotations
							)
						);
					}
				}
			}

			// Perform a sweep. Get a list of all of the positions
			// that are blank but have adjacent non-blank
			// Check if they have only one possible piece
			// (There is no end to how sophisticated this bit can get


			List<int> blanksWithAdjacentFills = [];
			for (int posIndex = 0; posIndex < listPlacements.Values.Count; ++posIndex)
			{
				if (listPlacements.Values[posIndex] == null)
				{
					if (
						PuzzleSolver.GetAdjacentPlacementIndexes(puzzleEnvironment, posIndex)
							.Where(adj => listPlacements.Values[adj] != null)
							.Any()
					)
					{
						blanksWithAdjacentFills.Add(posIndex);
					}
				}
			}
			foreach (int blankPosIndex in blanksWithAdjacentFills)
			{
				var edgeRequirements = PuzzleSolver.GetEdgeRequirements(
					puzzleEnvironment,
					listPlacements,
					blankPosIndex
				);
				// Work out, among all of the remaining pieces,
				// if there iz zero, one, or more than one possible 
				// choices for this position
				Placement? possiblePlacement = null;
				bool moreThanOne = false;
				for (int candidatePieceIndex = 0; candidatePieceIndex < 256; ++candidatePieceIndex)
				{
					if (listPlacements.ContainsPieceIndex(candidatePieceIndex))
					{
						continue;
					}
					var candidateEdges = puzzleEnvironment.PieceSides[candidatePieceIndex];
					var possibleRotations = edgeRequirements.SelectMany(
						er => PuzzleSolver.GetRotations(candidateEdges, er)
					).ToArray();
					if (possibleRotations.Any())
					{
						if (possiblePlacement == null)
						{
							possiblePlacement = new Placement(candidatePieceIndex, possibleRotations);
						}
						else
						{
							moreThanOne = true;
							break;
						}
					}
				}
				if (moreThanOne)
				{
					continue;
				}
				if (possiblePlacement == null)
				{
					return null;
				}
				return TryAddPiece(
					puzzleEnvironment,
					listPlacements,
					blankPosIndex,
					possiblePlacement.PieceIndex
				);
			}
			return listPlacements;
		}

	}
}

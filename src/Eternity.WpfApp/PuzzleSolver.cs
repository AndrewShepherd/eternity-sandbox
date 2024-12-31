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
		public static class EdgeIndexes
		{
			public const int Top = 0;
			public const int Right = 1;
			public const int Bottom = 2;
			public const int Left = 3;
		}

		public static ImmutableArray<int> Rotate(ImmutableArray<int> edges, Rotation rotation)
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

		public static IEnumerable<int> GetAdjacentPlacementIndexes(PuzzleEnvironment puzzleEnvironment, int placementIndex)
		{
			var thisPosition = puzzleEnvironment.PositionLookup[placementIndex];
			return GetAdjacentPositions(thisPosition)
				.Select(p => puzzleEnvironment.ReversePositionLookup[p]);
		}

		private static IEnumerable<int?> GetAdjacentSideColor(
			PuzzleEnvironment puzzleEnvironment,
			Position position,
			Placement?[] existingPlacements,
			int edgeIndex
		)
		{
			if (puzzleEnvironment.ReversePositionLookup.TryGetValue(position, out var placementIndex))
			{
				var adjacentPlacement = existingPlacements[placementIndex];
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

		public static IEnumerable<EdgeRequirements> GetEdgeRequirements(
			PuzzleEnvironment puzzleEnvironment,
			Placement?[] existingPlacements,
			int targetPositionIndex
		)
		{
			Func<int, int, int, IEnumerable<int?>> getAdjacentSideColor = (x, y, edgeIndex) => GetAdjacentSideColor(
				puzzleEnvironment,
				new Position(x, y),
				existingPlacements,
				edgeIndex
			);
			var target = puzzleEnvironment.PositionLookup[targetPositionIndex];

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

		internal static bool CanMatch(int? n1, int? n2) =>
			(n1, n2) switch
			{
				(null, _) => true,
				(_, null) => true,
				_ => n1 == n2
			};

		internal static bool CanMatch(EdgeRequirements r1, EdgeRequirements r2) =>
			CanMatch(r1.left, r2.left)
			&& CanMatch(r1.right, r2.right)
			&& CanMatch(r1.top, r2.top)
			&& CanMatch(r1.bottom, r2.bottom);

		static Rotation[] AllRotations = [
			Rotation.None,
			Rotation.Ninety,
			Rotation.OneEighty,
			Rotation.TwoSeventy
		];

		internal static Rotation[] GetPossibleRotations(
			PuzzleEnvironment puzzleEnvironment,
			int positionIndex,
			int pieceIndex,
			Placement?[] listPlacements
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

		internal static IEnumerable<Rotation> GetRotations(
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

	}
}

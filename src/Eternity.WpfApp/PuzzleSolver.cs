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


		private static int? GetAdjacentSideColor(
			PuzzleEnvironment puzzleEnvironment,
			Position position,
			List<Placement> existingPlacements,
			int edgeIndex
		)
		{
			if (puzzleEnvironment.ReversePositionLookup.TryGetValue(position, out var placementIndex))
			{
				if (placementIndex <= existingPlacements.Count)
				{
					var topPlacement = existingPlacements[placementIndex];
					var topColors = puzzleEnvironment.PieceSides[topPlacement.PieceIndex];
					var topColorsRotated = Rotate(topColors, topPlacement.Rotation);
					return topColorsRotated[edgeIndex];
				}
			}
			return default;
		}

		public static EdgeRequirements GetEdgeRequirements(
			PuzzleEnvironment puzzleEnvironment,
			List<Placement> existingPlacements,
			int targetPositionIndex
		)
		{
			Func<Position, int, int?> getAdjacentSideColor = (position, edgeIndex) => GetAdjacentSideColor(
				puzzleEnvironment,
				position,
				existingPlacements,
				edgeIndex
			);
			var target = puzzleEnvironment.PositionLookup[targetPositionIndex];
			int? topColor = null;
			int? leftColor = null;
			int? rightColor = null;
			int? bottomColor = null;
			if (target.Y == 0)
			{
				topColor = 23;
			}
			else
			{
				topColor = getAdjacentSideColor(
					new Position(target.X, target.Y - 1),
					EdgeIndexes.Bottom
				);
			}
			if (target.X == 0)
			{
				leftColor = 23;
			}
			else
			{
				leftColor = getAdjacentSideColor(
					new Position(target.X - 1, target.Y),
					EdgeIndexes.Right
				);
			}
			if (target.X == 15)
			{
				rightColor = 23;
			}
			else
			{
				rightColor = getAdjacentSideColor(
					new Position(target.X + 1, target.Y),
					EdgeIndexes.Left
				);
			}
			if (target.Y == 15)
			{
				bottomColor = 23;
			}
			else
			{
				bottomColor = getAdjacentSideColor(
					new Position(target.X, target.Y+1),
					EdgeIndexes.Top
				);
			}
			return new EdgeRequirements(leftColor, topColor, rightColor, bottomColor);
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

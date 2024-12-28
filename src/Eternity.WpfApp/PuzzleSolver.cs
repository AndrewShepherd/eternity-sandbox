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

		public static EdgeRequirements GetEdgeRequirements(
			PuzzleEnvironment puzzleEnvironment,
			Dictionary<Position, int> reversePositionLookup,
			List<Placement> existingPlacements,
			int targetPositionIndex
		)
		{
			var target = puzzleEnvironment.PositionLookup[targetPositionIndex];
			int? topColor = null;
			int? leftColor = null;
			int? rightColor = null;
			int? bottomColor = null;
			if (target.Y == 0)
			{
				topColor = 23;
			}
			if (target.X == 0)
			{
				leftColor = 23;
			}
			else
			{
				var leftPosition = new Position(target.X - 1, target.Y);
				if (reversePositionLookup.TryGetValue(leftPosition, out var placementIndex))
				{
					if (placementIndex <= existingPlacements.Count)
					{
						var leftPlacement = existingPlacements[placementIndex];
						var leftColors = puzzleEnvironment.PieceSides[leftPlacement.PieceIndex];
						var leftColorsRotated = Rotate(leftColors, leftPlacement.rotation);
						leftColor = leftColorsRotated[EdgeIndexes.Right];
					}
				}
			}
			if (target.X == 15)
			{
				rightColor = 23;
			}
			if (target.Y == 15)
			{
				bottomColor = 23;
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
				// Top, right, bottom, left
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

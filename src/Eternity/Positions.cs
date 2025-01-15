using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternity
{
	public static class Positions
	{
		[Obsolete("Using the hardcoded positions!")]
		public static IReadOnlyList<Position> PositionLookup
		{
			get;
		} = GeneratePositions(256);

		[Obsolete("Using the harcoded positions!")]
		public static IReadOnlyDictionary<Position, int> ReversePositionLookup
		{
			get;
		}  = PositionLookup.Select(
				(position, index) => KeyValuePair.Create(position, index)
		).ToDictionary();

		public static Position[] GeneratePositions(int length)
		{
			var sideLength = (int)Math.Sqrt(length);
			if (sideLength * sideLength != length)
			{
				throw new Exception("Generating positions - not a square number!");
			}
			var rv = new Position[length];
			var targetIndex = 0;

			int minRow = 0;
			int maxRow = sideLength - 1;
			int minCol = 0;
			int maxCol = sideLength - 1;
			while ((minRow <= maxRow) && (minCol <= maxCol))
			{
				for (var x = minCol; x <= maxCol; ++x)
				{
					rv[targetIndex++] = new Position(x, minRow);
				}
				minRow += 1;
				for (var y = minRow; y <= maxRow; ++y)
				{
					rv[targetIndex++] = new Position(maxCol, y);
				}
				maxCol -= 1;
				for (var x = maxCol; x >= minCol; --x)
				{
					rv[targetIndex++] = new Position(x, maxRow);
				}
				maxRow -= 1;
				for (var y = maxRow; y >= minRow; --y)
				{
					rv[targetIndex++] = new Position(minCol, y);
				}
				minCol += 1;
			}
			return rv;
		}

		public static Position Above(Position p) => new Position(p.X, p.Y - 1);
		public static Position Below(Position p) => new Position(p.X, p.Y + 1);
		public static Position Left(Position p) => new Position(p.X - 1, p.Y);
		public static Position Right(Position p) => new Position(p.X + 1, p.Y);
	}
}

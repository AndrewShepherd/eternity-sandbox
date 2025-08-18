namespace Eternity;

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


	private static Position[] GenerateOnionPositions(int length)
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

	private static Position[] GenerateAlternatingOnionPositions(int length)
	{
		var onion = GenerateOnionPositions(length);
		var result = new Position[length];
		int outIndex = 0;
		for(int i = 0; i < length; i += 2)
		{
			result[outIndex++] = onion[i];
		}
		for(int i = 1; i <length; i += 2)
		{
			result[outIndex++] = onion[i];
		}
		return result;
	}

	public static Position[] GenerateReverseScanningPositions(int length)
	{
		var sideLength = (int)Math.Sqrt(length);
		if (sideLength * sideLength != length)
		{
			throw new Exception("Generating positions - not a square number!");
		}
		var rv = new Position[length];
		var targetIndex = 0;
		for (int row = 0; row < sideLength; ++row)
		{
			if (row % 2 == 0)
			{
				for(int col = 0; col < sideLength; ++col)
				{
					rv[targetIndex++] = new Position(col, row);
				}
			}
			else
			{
				for(int col = sideLength - 1; col >= 0; --col)
				{
					rv[targetIndex++] = new Position(col, row);
				}
			}
		}
		return rv;
	}

	public static Position[] GenerateAlternatingLinePositions(int length)
	{
		var sideLength = (int)Math.Sqrt(length);
		if (sideLength * sideLength != length)
		{
			throw new Exception("Generating positions - not a square number!");
		}
		var rv = new Position[length];
		const int groupSize = 2;
		var targetIndex = 0;

		for (int startRow = 0; startRow < groupSize; startRow += 1)
		{
			bool forwards = false;
			for (int row = startRow; row < sideLength; row += groupSize)
			{
				forwards = !forwards;
				if (forwards)
				{
					for (int col = 0; col < sideLength; ++col)
					{
						rv[targetIndex++] = new Position(col, row);
					}
				}
				else
				{
					for (int col = sideLength - 1; col >= 0; --col)
					{
						rv[targetIndex++] = new Position(col, row);
					}
				}
			}
		}
		return rv;
	}

	public static Position[] GenerateChessBoardPositions(int length)
	{
		var sideLength = (int)Math.Sqrt(length);
		if (sideLength * sideLength != length)
		{
			throw new Exception("Generating positions - not a square number!");
		}
		var rv = new Position[length];
		var targetIndex = 0;
		for(int row = 0; row < sideLength; ++row)
		{
			for(int col = (row)%2; col < sideLength; col += 2)
			{
				rv[targetIndex++] = new Position(col, row);
			}
		}
		for (int row = 0; row < sideLength; ++row)
		{
			for (int col = (row+1) % 2; col < sideLength; col += 2)
			{
				rv[targetIndex++] = new Position(col, row);
			}
		}
		return rv;
	}

	public static Position[] GeneratePositions(int length)
	{
		return GenerateOnionPositions(length);
	}

	public static Position Above(Position p) => new Position(p.X, p.Y - 1);
	public static Position Below(Position p) => new Position(p.X, p.Y + 1);
	public static Position Left(Position p) => new Position(p.X - 1, p.Y);
	public static Position Right(Position p) => new Position(p.X + 1, p.Y);
}

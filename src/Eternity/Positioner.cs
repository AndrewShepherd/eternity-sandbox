namespace Eternity
{
	public interface IPositioner
	{
		public (Position, IPositioner) GetNext();
	}


	public class Positioner
	{
		public required IReadOnlyList<Position> PositionLookup { get; init; }

		public required Dimensions Dimensions { get; init; }

		public static Positioner Generate(int pieceCount)
		{
			var positions = Positions.GeneratePositions(pieceCount);
			var sideLength = (int)Math.Sqrt(pieceCount);
			return new Positioner
			{
				PositionLookup = positions,
				Dimensions = new(sideLength, sideLength)
			};
		}
	}
}

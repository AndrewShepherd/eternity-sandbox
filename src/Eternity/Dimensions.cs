namespace Eternity
{
	public record class Dimensions(int Width, int Height);

	public static class DimensionExtensions
	{
		public static bool Contains(this Dimensions d, Position position) =>
			position.X >= 0
			&& position.X < d.Width
			&& position.Y >= 0
			&& position.Y < d.Height;

		public static Position IndexToPosition(this Dimensions d, int index) =>
			new Position(index % d.Width, index / d.Width);

		public static int PositionToIndex(this Dimensions d, Position p) =>
			p.Y * d.Width + p.X;
	}
}

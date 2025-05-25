namespace Eternity
{
	public record class Position(int X, int Y) : IComparable<Position>
	{
		int IComparable<Position>.CompareTo(Position? other)
		{
			if (ReferenceEquals(this, other))
			{
				return 0;
			}
			if (other == null)
			{
				throw new Exception("Not expecting a null position");
			};
			if (this.Equals(other))
			{
				return 0;
			}
			if (this < other)
			{
				return -1;
			}
			return 1;
		}

		public static bool operator <(Position p1, Position p2) =>
				(p1.X == p2.X)
				? p1.Y < p2.Y
				: p1.X < p2.X;

		public static bool operator >(Position p1, Position p2) =>
				(p1.X == p2.X)
				? p1.Y > p2.Y
				: p1.X > p2.X;
	}

	

}

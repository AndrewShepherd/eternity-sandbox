namespace Eternity
{
	public class Placement(Position position, int pieceIndex, Rotation[] rotations)
	{
		public Position Position => position;
		public int PieceIndex => pieceIndex;
		public Rotation[] Rotations => rotations;
	}
}

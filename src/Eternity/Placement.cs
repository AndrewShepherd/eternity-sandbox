namespace Eternity
{
	public class Placement(int pieceIndex, Rotation[] rotations)
	{
		public int PieceIndex => pieceIndex;
		public Rotation[] Rotations => rotations;
	}
}

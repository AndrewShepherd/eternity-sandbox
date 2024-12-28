namespace Eternity
{
	public class PuzzlePiece(
		int index,
		int[] sides,
		string imageId
	)
	{
		public int Index => index;
		public int[] Sides => sides;

		public string ImageId => imageId;
	}
}

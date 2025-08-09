namespace Eternity.WpfApp
{
	public class PuzzleEnvironment
	{
		private PuzzleEnvironment()
		{
		}

		public IReadOnlyList<IReadOnlyList<ulong>> PieceSides = [];

		public static async Task<PuzzleEnvironment> Generate()
		{
			var pieces = await PuzzleProvider.LoadPieces();
			return new PuzzleEnvironment
			{
				PieceSides = pieces.Select(
					p => p.Sides
				).Select(
					s => s.ToList()
				).ToList(),
			};
		}
	}
}

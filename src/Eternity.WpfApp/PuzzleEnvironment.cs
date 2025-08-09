namespace Eternity.WpfApp
{
	using System.Collections.Immutable;

	public class PuzzleEnvironment
	{
		private PuzzleEnvironment()
		{
		}

		public IReadOnlyList<ImmutableArray<ulong>> PieceSides = [];

		public static async Task<PuzzleEnvironment> Generate()
		{
			var pieces = await PuzzleProvider.LoadPieces();
			return new PuzzleEnvironment
			{
				PieceSides = pieces.Select(p => p.Sides).Select(s => s.ToImmutableArray()).ToImmutableArray(),
			};
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternity.WpfApp
{
	internal sealed class SolutionState
	{
		private PlacementStack _placementStack = new PlacementStack();
		private int _badSequenceIndex = -1;

		private readonly PuzzleEnvironment _puzzleEnvironment;

		public int BadSequenceIndex => _badSequenceIndex;

		public Placements SetSequence(IReadOnlyList<int> sequence)
		{
			var pieceIndexes = Sequence.GeneratePieceIndexes(sequence);
			(int successfulAdds, Placements placements) = _placementStack.ApplyPieceOrder(
				_puzzleEnvironment,	
				Sequence.GeneratePieceIndexes(sequence)
			);
			_badSequenceIndex = Sequence.ListPlacementIndexToSequenceIndex(successfulAdds);
			return placements;
		}

		public SolutionState(PuzzleEnvironment puzzleEnvironment)
		{
			_puzzleEnvironment = puzzleEnvironment;
		}
	}
}

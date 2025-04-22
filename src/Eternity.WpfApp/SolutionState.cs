using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;

namespace Eternity.WpfApp
{
	internal sealed class SolutionState
	{
		private PlacementStack _placementStack = new PlacementStack();
		private int _badSequenceIndex = -1;

		private readonly IReadOnlyList<ImmutableArray<int>> _pieceSides;

		public int BadSequenceIndex => _badSequenceIndex;

		public Placements SetSequence(IReadOnlyList<int> sequence)
		{
			var pieceIndexes = Sequence.GeneratePieceIndexes(sequence);
			(int successfulAdds, Placements placements) = _placementStack.ApplyPieceOrder(
				this._pieceSides,	
				sequence
			);
			_badSequenceIndex = Sequence.ListPlacementIndexToSequenceIndex(successfulAdds);
			return placements;
		}

		private readonly SequenceSpecs _sequenceSpecs;
		public SolutionState(IReadOnlyList<ImmutableArray<int>> pieceSides)
		{
			_sequenceSpecs = new SequenceSpecs(pieceSides.Count);
			_pieceSides = pieceSides;
		}
	}
}

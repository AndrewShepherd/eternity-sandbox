using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternity
{
	// An immutable, indexable list of Placement
	//
	// When you add an item, it will create a new object
	// Enforces a constraint that you cannot add an item
	// to an index that's already created
	public class Placements
	{
		private static ImmutableArray<Placement?> EmptyPlacements = new Placement?[256].ToImmutableArray();
		private static ImmutableArray<bool> NoUsedPieceIndexes = new bool[256].ToImmutableArray();
		private ImmutableArray<Placement?> _placements = EmptyPlacements;
		private ImmutableArray<bool> _usedPieceIndexes = NoUsedPieceIndexes;

		private ImmutableArray<SquareConstraint> _constraints = Enumerable.Range(0, 256).Select(
			_ => SquareConstraint.Initial
		).ToImmutableArray();

		public IReadOnlyList<SquareConstraint> Constraints => _constraints;

		public readonly static Placements Empty = new Placements();

		public Placements SetItem(int positionIndex, Placement placement)
		{
			var placementAlreadyThere = _placements[positionIndex];

			if (placementAlreadyThere == null)
			{
				if (_usedPieceIndexes[placement.PieceIndex])
				{
					throw new Exception("Attempting to place the same piece twice");
				}
			}
			else
			{
				if (placementAlreadyThere.PieceIndex != placement.PieceIndex)
				{
					throw new Exception("Attempting to set a position that's already been set");
				}
			}
			return new Placements
			{
				_placements = _placements.SetItem(positionIndex, placement),
				_usedPieceIndexes = _usedPieceIndexes.SetItem(placement.PieceIndex, true),
				_constraints = _constraints
			};
		}

		public IReadOnlyList<Placement?> Values => _placements;
		public bool ContainsPieceIndex(int pieceIndex) => _usedPieceIndexes[pieceIndex];

		private Placements()
		{
		}
	}
}

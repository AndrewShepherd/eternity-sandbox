﻿namespace Eternity
{
	using System.Collections.Immutable;

	// An immutable, indexable list of Placement
	//
	// Enforces a number of constraints
	//
	// - You cannot place an item where an item already is
	// - You cannot place the same item in multiple positions
	//
	public class Placements
	{
		private static ImmutableArray<bool> NoUsedPieceIndexes = new bool[256].ToImmutableArray();
		private ImmutableArray<Placement?> _placements = [];
		private ImmutableArray<bool> _usedPieceIndexes = NoUsedPieceIndexes;

		private Constraints _constraints;

		public Constraints Constraints => _constraints;

		public Placements? SetItem(int positionIndex, Placement placement)
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

			return _constraints.SetPlacement(
				this.Positioner.PositionLookup[positionIndex], 
				placement
			) switch
			{
				Constraints c =>
					new Placements
					{
						Positioner = this.Positioner,
						Dimensions = this.Dimensions,
						PieceSides = this.PieceSides,
						_placements = _placements.SetItem(positionIndex, placement),
						_usedPieceIndexes = _usedPieceIndexes.SetItem(placement.PieceIndex, true),
						_constraints = c
					},
				_ => null
			};
		}

		public IReadOnlyList<Placement?> Values => _placements;

		public required IReadOnlyList<ImmutableArray<int>> PieceSides { get; init; }
		public required Positioner Positioner { get; init; }

		public required Dimensions Dimensions { get; init; }

		public bool ContainsPieceIndex(int pieceIndex) => _usedPieceIndexes[pieceIndex];

		private Placements()
		{
		}

		public static Placements CreateInitial(IReadOnlyList<ImmutableArray<int>> pieceSides)
		{
			Constraints? initialConstraints = Constraints.Generate(pieceSides);

			if (initialConstraints == null)
			{
				throw new Exception("Unable to generate the initial constraints");
			}
			else
			{
				var sqrt = (int)Math.Round(Math.Sqrt(pieceSides.Count));
				return new Placements
				{
					Positioner = Positioner.Generate(pieceSides.Count),
					Dimensions = new Dimensions(sqrt, sqrt),
					_placements = new Placement?[pieceSides.Count].ToImmutableArray(),
					PieceSides = pieceSides,
					_constraints = initialConstraints
				};
			}
		}
	}
}

namespace Eternity
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
		private ImmutableList<Placement> _placements = ImmutableList<Placement>.Empty;

		private Constraints _constraints;

		public Constraints Constraints => _constraints;

		public Placements? SetItem(Placement placement)
		{
			return _constraints.SetPlacement(
				placement
			) switch
			{
				Constraints c =>
					new Placements
					{
						Dimensions = this.Dimensions,
						PieceSides = this.PieceSides,
						_placements = _placements.Add(placement),
						_constraints = c
					},
				_ => null
			};
		}

		public IReadOnlyList<Placement> Values => _placements;

		public required IReadOnlyList<IReadOnlyList<ulong>> PieceSides { get; init; }

		public required Dimensions Dimensions { get; init; }


		private Placements()
		{
		}

		public static Placements CreateInitial(IReadOnlyList<IReadOnlyList<ulong>> pieceSides)
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
					Dimensions = new Dimensions(sqrt, sqrt),
					_placements = ImmutableList<Placement>.Empty,
					PieceSides = pieceSides,
					_constraints = initialConstraints
				};
			}
		}	
	}
}

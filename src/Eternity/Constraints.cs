using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternity
{
	public class Constraints
	{
		private readonly Dimensions _dimensions;
		private readonly ImmutableArray<SlotConstraint> _constraints;

		private Constraints(Dimensions dimensions, ImmutableArray<SlotConstraint> constraints)
		{
			_dimensions = dimensions;
			_constraints = constraints;
		}

		public Constraints? SetPlacement(Position position, Placement placement)
		{
			var newConstraints = _constraints.SetPlacement(
				position,
				placement,
				_dimensions
			);
			return newConstraints switch
			{
				ImmutableArray<SlotConstraint> l => new Constraints(_dimensions, l),
				_ => default
			};
		}

		public static Constraints? Generate(IReadOnlyList<ImmutableArray<int>> pieceSides)
		{
			var sqrt = (int)Math.Round(Math.Sqrt(pieceSides.Count));
			var dimensions = new Dimensions(sqrt, sqrt);
			var initialPlacements = SquareConstraintExtensions.GenerateInitialPlacements(pieceSides);
			return initialPlacements switch
			{
				ImmutableArray<SlotConstraint> p =>
					new Constraints(
						dimensions,
						p
					),
				_ => null
			};
		}

		public SlotConstraint At(Position pos) =>
			this._constraints[_dimensions.PositionToIndex(pos)];
	}
}

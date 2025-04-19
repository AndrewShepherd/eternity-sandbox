using System.Collections.Immutable;

namespace Eternity
{
	public interface IPositioner
	{
		public (Position, IPositioner) GetNext(Constraints constratints);
	}

	public class DynamicPositioner(Dimensions dimensions) : IPositioner
	{
		private ImmutableHashSet<Position> _returnedAlready = ImmutableHashSet<Position>.Empty;
		(Position, IPositioner) IPositioner.GetNext(Constraints constratints)
		{
			Position? result = null;
			foreach(var position in dimensions.GetAllPositions())
			{
				if (_returnedAlready.Contains(position))
				{
					continue;
				}
				if (result == null)
				{
					result = position;
					var c = constratints.At(position);
					if (c.Pieces.Count() == 1)
					{
						break;
					}
					continue;
				}
				var reigningConstraint = constratints.At(result);
				var newConstraint = constratints.At(position);
				
				if (newConstraint.Pieces.Count < reigningConstraint.Pieces.Count)
				{
					result = position;
					if (newConstraint.Pieces.Count == 1)
					{
						break;
					}
				}
			}
			if (result != null)
			{
				return (
					result,
					new DynamicPositioner(dimensions)
					{
						_returnedAlready = this._returnedAlready.Add(result)
					}
				);
			}
			else
			{
				throw new Exception("Invoked dynamic positioner next when no more items");
			}

		}
	}


	public class Positioner
	{
		[Obsolete]
		public required IReadOnlyList<Position> PositionLookup { get; init; }

		public required Dimensions Dimensions { get; init; }

		public static Positioner Generate(int pieceCount)
		{
			var positions = Positions.GeneratePositions(pieceCount);
			var sideLength = (int)Math.Sqrt(pieceCount);
			return new Positioner
			{
				PositionLookup = positions,
				Dimensions = new(sideLength, sideLength)
			};
		}
	}
}

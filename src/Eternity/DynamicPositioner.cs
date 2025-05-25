namespace Eternity
{
	using System.Collections.Immutable;
	using System.Data;

	public class DynamicPositioner(Dimensions dimensions) : IPositioner
	{
		private ImmutableHashSet<Position> _returnedAlready = ImmutableHashSet<Position>.Empty;

		internal static Position? Select(IEnumerable<Position> positions, Constraints constraints)
		{
			Position? result = null;
			foreach (var position in positions)
			{
				if (result == null)
				{
					result = position;
					var c = constraints.At(position);
					if (c.Pieces.Count == 1)
					{
						break;
					}
					continue;
				}
				var reigningConstraint = constraints.At(result);
				var newConstraint = constraints.At(position);

				if (newConstraint.Pieces.Count < reigningConstraint.Pieces.Count)
				{
					result = position;
					if (newConstraint.Pieces.Count == 1)
					{
						break;
					}
				}
			}
			return result;
		}

		(Position, IPositioner) IPositioner.GetNext(Constraints constraints)
		{
			Position? result = Select(
				dimensions.GetAllPositions().Where(p => !_returnedAlready.Contains(p)),
				constraints
			);
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


	public class DynamicPositionerAdjacentsOnly(Dimensions dimensions) : IPositioner
	{
		internal ImmutableHashSet<Position> _returnedAlready = ImmutableHashSet<Position>.Empty;
		internal ImmutableSortedSet<Position> _adjacentPositions = ImmutableSortedSet<Position>.Empty;

		(Position, IPositioner) IPositioner.GetNext(Constraints constraints)
		{
			Position? nextPosition = _adjacentPositions.IsEmpty
				? DynamicPositioner.Select(
					dimensions.GetAllPositions().Where(p => !_returnedAlready.Contains(p)),
					constraints
				)
				: DynamicPositioner.Select(
					_adjacentPositions,
					constraints
				);
			if (nextPosition == null)
			{
				throw new Exception("Invoked dynamic positioner next when no more items");
			}
			Position[] unfilteredAdjacents = [
				new(nextPosition.X - 1, nextPosition.Y),
				new(nextPosition.X + 1, nextPosition.Y),
				new(nextPosition.X, nextPosition.Y - 1),
				new(nextPosition.X, nextPosition.Y + 1)
			];
			var filteredAdjacents = unfilteredAdjacents.Where(
				p => dimensions.Contains(p) && !_returnedAlready.Contains(p)
			);
			return (
				nextPosition,
				new DynamicPositionerAdjacentsOnly(dimensions)
				{
					_adjacentPositions = this._adjacentPositions.Union(filteredAdjacents).Remove(nextPosition),
					_returnedAlready = this._returnedAlready.Add(nextPosition),
				}
			);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternity
{
	public class Positioner
	{
		public required IReadOnlyList<Position> PositionLookup { get; init; }
		public required IDictionary<Position, int> ReversePositionLookup { get; init; }

		public static Positioner Generate(int pieceCount)
		{
			var positions = Positions.GeneratePositions(pieceCount);
			var reverseLookup = positions.Select(
				(position, index) => KeyValuePair.Create(position, index)
			).ToDictionary();
			return new Positioner
			{
				PositionLookup = positions,
				ReversePositionLookup = reverseLookup,
			};
		}
	}
}

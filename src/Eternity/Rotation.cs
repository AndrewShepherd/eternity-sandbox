using System.Collections.Immutable;
using System.Diagnostics.Tracing;

namespace Eternity
{
	public enum Rotation
	{
		None = 0,
		Ninety = 1,
		OneEighty = 2,
		TwoSeventy = 3
	};

	public static class RotationExtensions
	{
		public static IEnumerable<Rotation> AllRotations = [
			Rotation.None,
			Rotation.Ninety,
			Rotation.OneEighty,
			Rotation.TwoSeventy
		];

		public static ImmutableArray<int> Rotate(IReadOnlyList<int> edges, Rotation rotation)
		{
			var result = new int[edges.Count];
			for (int i = 0; i < edges.Count; i++)
			{
				result[(i + (int)rotation) % edges.Count] = edges[i];
			}
			return result.ToImmutableArray();
		}
	}
}
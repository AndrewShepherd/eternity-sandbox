namespace Eternity;

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

	public static void Rotate(IReadOnlyList<ulong> edges, Rotation rotation, Span<ulong> result)
	{
		for (int i = 0; i < edges.Count; i++)
		{
			result[(i + (int)rotation) % edges.Count] = edges[i];
		}
	}
}
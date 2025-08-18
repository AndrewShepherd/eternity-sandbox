namespace Eternity;

public record class EdgeRequirements(int? left, int? top, int? right, int? bottom);

public static class EdgeIndexes
{
	public const int Top = 0;
	public const int Right = 1;
	public const int Bottom = 2;
	public const int Left = 3;
}

public static class EdgeRequirementsExtensions
{
	private static bool CanMatch(int? n1, int? n2) =>
		(n1, n2) switch
		{
			(null, _) => true,
			(_, null) => true,
			_ => n1 == n2
		};

	public static bool CanMatch(this EdgeRequirements r1, EdgeRequirements r2) =>
		CanMatch(r1.left, r2.left)
		&& CanMatch(r1.right, r2.right)
		&& CanMatch(r1.top, r2.top)
		&& CanMatch(r1.bottom, r2.bottom);
}

namespace Eternity;

public sealed class PreRotatedPatternLookup
{
	private PreRotatedPatternLookup(ulong[] source)
	{
		_source = source;
	}

	private readonly ulong[] _source;

	public static PreRotatedPatternLookup Generate(IReadOnlyList<IReadOnlyList<ulong>> source)
	{
		var pieceCount = source.Count;
		var totalSpanSize = pieceCount * 4 * 4;
		var allData = new ulong[totalSpanSize];
		ulong[] rotatedPattern = new ulong[4];
		for (int i = 0; i < source.Count; ++i)
		{
			var piecePattern = source[i];
			int j = 0;
			foreach (var rotation in RotationExtensions.AllRotations)
			{
				RotationExtensions.Rotate(piecePattern, rotation, rotatedPattern);
				Array.Copy(rotatedPattern, 0, allData, i * 16 + j * 4, 4);
				++j;
			}
		}
		return new(allData);
	}

	public Span<ulong> ForPiece(int pieceIndex) =>
		new Span<ulong>(_source, pieceIndex * 16, 16);

}

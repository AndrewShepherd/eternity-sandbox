namespace Eternity;

using System.Text.RegularExpressions;

public static partial class PieceTextReader
{
	public static IReadOnlyList<ulong>[] Parse(string source)
	{
		var allStrings = RegexFourLetters.Matches(source).Select(m => m.Groups[1].Value).ToArray();
		return allStrings.Select(
				(s, i) => PuzzleProvider.ConvertToSides(s).ToList()
			).ToArray();
	}

	[GeneratedRegex(@"\b([A-Z,a-z]{4})\b")]
	private static partial Regex RegexFourLetters { get; }
}

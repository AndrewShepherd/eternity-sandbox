using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Eternity
{
	public static class PieceTextReader
	{
		public static ImmutableArray<ulong>[] Parse(string source)
		{
			var regex = new Regex(@"\b([A-Z,a-z]{4})\b");
			var allStrings = regex.Matches(source).Select(m => m.Groups[1].Value).ToArray();
			return allStrings.Select(
					(s, i) => PuzzleProvider.ConvertToSides(s).ToImmutableArray()
				).ToArray();
		}
	}
}

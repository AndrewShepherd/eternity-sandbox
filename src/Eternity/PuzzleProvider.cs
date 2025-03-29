using System.Text.Json;

namespace Eternity
{
	public static class PuzzleProvider
	{
		const string JsonResource = "Eternity.Resources.pieces.json";

		private static int CharToNumber(char c) =>
			c switch
			{
				>= 'a' and < 'z' => c - 'a',
				'A' => 22,
				_ => c - 'B' + 24
			};

		public static int[] ConvertToSides(string s) =>
			s.ToCharArray()
			.Select(
				CharToNumber
			).ToArray();
		private static IEnumerable<Tile> ExtractPieces(JsonDocument document)
		{
			var rootElement = document.RootElement;
			foreach(var element in rootElement.EnumerateArray())
			{
				var codeProperty = element.GetProperty("code");
				yield return new Tile(
					element.GetProperty("index").GetInt32(),
					ConvertToSides(codeProperty.GetString()!)
				);
			}
		}
		public static async Task<Tile[]> LoadPieces()
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();

			var mri = assembly.GetManifestResourceInfo("Eternity.Resources.pieces.json");
			using (var stream = assembly.GetManifestResourceStream(JsonResource))
			{
				if (stream == null)
				{
					throw new Exception($"Unable to load from {JsonResource}");
				}
				var document = await System.Text.Json.JsonDocument.ParseAsync(stream);
				return ExtractPieces(document).ToArray();
			}
		}
	}
}

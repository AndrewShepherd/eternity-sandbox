using System.Text.Json;

namespace Eternity
{
	public static class PuzzleProvider
	{
		const string JsonResource = "Eternity.Resources.pieces.json";

		private static int[] ConvertToSides(string s) =>
			s.ToCharArray()
			.Select(
				c => c - 'a'
			).ToArray();
		private static IEnumerable<PuzzlePiece> ExtractPieces(JsonDocument document)
		{
			var rootElement = document.RootElement;
			foreach(var element in rootElement.EnumerateArray())
			{
				var codeProperty = element.GetProperty("code");
				var imageNameProperty = element.GetProperty("imageName");
				yield return new PuzzlePiece(
					element.GetProperty("index").GetInt32(),
					ConvertToSides(codeProperty.GetString()!),
					imageNameProperty.GetString()!
				);
			}
		}
		public static async Task<PuzzlePiece[]> LoadPieces()
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

namespace Eternity
{
	public static class ImageProvider
	{
		public static IEnumerable<string> GetResourceNames()
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			return assembly.GetManifestResourceNames();
		}

		public static Stream? Load(string imageId) =>
			System.Reflection.Assembly.GetExecutingAssembly()
				.GetManifestResourceStream($"Eternity.Resources.{imageId}");
	}
}

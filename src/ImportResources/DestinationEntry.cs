

namespace ImportResources
{
	public class DestinationEntry
	{
		public int Index { get; set; }
		public required string Code { get; init; } = String.Empty;

		public required string ImageName { get; init; }
	}
}

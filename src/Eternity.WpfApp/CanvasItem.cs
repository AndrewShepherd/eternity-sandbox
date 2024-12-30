
namespace Eternity.WpfApp
{
	using System;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;

	public class CanvasItem
	{
		public BitmapImage? ImageSource { get; set; }

		public double Top { get; set; }
		public double Left { get; set; }
		public int Rotation { get; internal set; }
	}

	public static class CanvasItemExtensions
	{
		internal static Func<Placement, int, CanvasItem> CreateCanvasItemGenerator(
			PuzzleEnvironment environment
		)
		{
			var firstImage = environment.Images[0];
			var imageWidth = firstImage.Width;
			var imageHeight = firstImage.Height;
			return (placement, index) =>
			{
				var position = environment.PositionLookup[index];
				return new CanvasItem
				{
					ImageSource = environment.Images[placement.PieceIndex],
					Left = position.X * imageWidth,
					Top = position.Y * imageHeight,
					Rotation = (int)placement.Rotations[0] * 90,
				};
			};
		} 

		internal static IEnumerable<CanvasItem> GenerateCanvasItems(PuzzleEnvironment environment, Placement?[] placements)
		{
			var g = CreateCanvasItemGenerator(environment);
			for(int i = 0; i < placements.Length; ++i)
			{
				var item = placements[i];
				if (item != null)
				{
					yield return g(item, i);
				}
			}
		}
	}
}

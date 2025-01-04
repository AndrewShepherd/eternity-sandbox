
namespace Eternity.WpfApp
{
	using System;
	using System.Windows.Media.Imaging;

	public abstract class CanvasItem
	{
	}

	public class CanvasPieceItem : CanvasItem
	{
		public BitmapImage? ImageSource { get; set; }

		public double Top { get; set; }
		public double Left { get; set; }
		public int Rotation { get; internal set; }
	}

	public sealed class CanvasHighlightItem : CanvasItem
	{
		public double Top { get; set; }
		public double Left { get; set; }

		public double Width { get; set; }

		public double Height { get; set; }
	}

	public static class CanvasItemExtensions
	{
		internal static Func<Placement, int, CanvasPieceItem> CreateCanvasItemGenerator(
			IReadOnlyList<BitmapImage> images
		)
		{
			var firstImage = images[0];
			var imageWidth = firstImage.Width;
			var imageHeight = firstImage.Height;
			return (placement, index) =>
			{
				var position = Positions.PositionLookup[index];
				return new CanvasPieceItem
				{
					ImageSource = images[placement.PieceIndex],
					Left = position.X * imageWidth,
					Top = position.Y * imageHeight,
					Rotation = (int)placement.Rotations[0] * 90,
				};
			};
		} 

		internal static IEnumerable<CanvasPieceItem> GenerateCanvasPieceItems(
			IReadOnlyList<BitmapImage> images,
			IReadOnlyList<Placement?> placements)
		{
			var g = CreateCanvasItemGenerator(images);
			for(int i = 0; i < placements.Count; ++i)
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

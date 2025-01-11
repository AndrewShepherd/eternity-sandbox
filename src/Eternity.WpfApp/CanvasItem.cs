
namespace Eternity.WpfApp
{
	using System;
	using System.Windows.Media.Imaging;

	public interface CanvasItem
	{
		double Top { get;}
		double Left { get; }

		double Width { get; }

		double Height { get; }
	}

	public class CanvasPieceItem : CanvasItem
	{
		public BitmapImage? ImageSource { get; set; }

		public double Top { get; init; }
		public double Left { get; init; }

		public double Width { get; init; }

		public double Height { get; init; }
		public int Rotation { get; internal set; }
	}

	public sealed class CanvasHighlightItem : CanvasItem
	{
		public double Top { get; init; }
		public double Left { get; init; }

		public double Width { get; init; }

		public double Height { get; init; }
	}

	public sealed class CanvasConstraintItem : CanvasItem
	{
		public double Top { get; init; }
		public double Left { get; init; }

		public double Width { get; init; }

		public double Height { get; init; }

		public int Count { get; init; }
	}

	public static class CanvasItemExtensions
	{
		internal static Func<Placement, int, CanvasPieceItem> CreateCanvasPieceItemGenerator(
			double imageWidth,
			double imageHeight,
			IReadOnlyList<BitmapImage> images
		)
		{
			return (placement, index) =>
			{
				var position = Positions.PositionLookup[index];
				return new CanvasPieceItem
				{
					ImageSource = images[placement.PieceIndex],
					Left = position.X * imageWidth,
					Top = position.Y * imageHeight,
					Width = imageWidth,
					Height = imageHeight,
					Rotation = (int)placement.Rotations[0] * 90,
				};
			};
		}

		internal static IEnumerable<CanvasConstraintItem> GenerateCanvasConstraintItem(
			double squareWidth,
			double squareHeight,
			IReadOnlyList<SquareConstraint> constraints
		)
		{
			for(int i = 0; i < constraints.Count; ++i)
			{
				var position = Positions.PositionLookup[i];
				yield return new CanvasConstraintItem
				{
					Left = position.X * squareWidth,
					Top = position.Y * squareHeight,
					Width = squareWidth,
					Height = squareHeight,
					Count = constraints[i].Pieces.Count
				};
			}
		}

		internal static IEnumerable<CanvasPieceItem> GenerateCanvasPieceItems(
			double pieceWidth,
			double pieceHeight,
			IReadOnlyList<BitmapImage> images,
			IReadOnlyList<Placement?> placements)
		{
			var g = CreateCanvasPieceItemGenerator(pieceWidth, pieceHeight, images);
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

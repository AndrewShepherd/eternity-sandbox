
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
		internal static Func<
			Placement, 
			int, 
			IEnumerable<CanvasPieceItem>
		> CreateCanvasPieceItemGenerator(
			double imageWidth,
			double imageHeight,
			IReadOnlyList<BitmapImage?> triangleImages,
			Placements placements
		)
		{
			return (placement, index) =>
			{
				var position = Positions.PositionLookup[index];
				var left = position.X * imageWidth;
				var top = position.Y * imageHeight;
				var pieceSides = placements.PieceSides[placement.PieceIndex];
				return pieceSides.Select(
					(pieceSideId, index) =>
					{
						// Not sure about the rotation
						return new CanvasPieceItem
						{
							ImageSource = triangleImages[pieceSideId]!,
							Height = imageHeight,
							Left = left,
							Top = top,
							Width = imageWidth,
							Rotation = ((int)placement.Rotations[0] + index) % 4 * 90
						};
					}
				);
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
				if (constraints[i].Pieces.Count == 1)
				{
					continue;
				}
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
			IReadOnlyList<BitmapImage?> triangleImages,
			Placements placements)
		{
			var g = CreateCanvasPieceItemGenerator(pieceWidth, pieceHeight, triangleImages, placements);
			for(int i = 0; i < placements.Values.Count; ++i)
			{
				var placement = placements.Values[i];
				if (placement != null)
				{
					foreach(var item in g(placement, i))
					{
						yield return item;
					}
				}
			}
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using System.Collections.Immutable;
using System.IO;

namespace Eternity.WpfApp
{
	public class PuzzleEnvironment
	{
		private PuzzleEnvironment()
		{

		}

		public ImmutableArray<BitmapImage> Images 
		{ 
			get;
			private init;
		}

		public ImmutableArray<Position> PositionLookup
		{
			get;
			private init;
		}

		public ImmutableArray<ImmutableArray<int>> PieceSides;

		private static Position[] GeneratePositions()
		{
			var rv = new Position[256];
			rv[0] = new Position(0, 0);
			rv[1] = new Position(15, 0);
			rv[2] = new Position(15, 15);
			rv[3] = new Position(0, 15);
			var targetIndex = 4;
			for (var x = 1; x < 15; ++x)
			{
				rv[targetIndex++] = new Position(x, 0);
			}
			for(var y = 1; y < 15; ++y)
			{
				rv[targetIndex++] = new Position(15, y);
			}
			for (var x = 14; x > 0; --x)
			{
				rv[targetIndex++] = new Position(x, 15);
			}
			for (var y = 14; y > 0; --y)
			{
				rv[targetIndex++] = new Position(0, y);
			}
			int minRow = 1;
			int maxRow = 14;
			int minCol = 1;
			int maxCol = 14;
			while ((minRow <= maxRow) && (minCol <= maxCol))
			{
				for(var x = minCol; x <= maxCol; ++x)
				{
					rv[targetIndex++] = new Position(x, minRow);
				}
				minRow += 1;
				for(var y = minRow; y <= maxRow; ++y)
				{
					rv[targetIndex++] = new Position(maxCol, y);
				}
				maxCol -= 1;
				for(var x = maxCol; x >= minCol; --x)
				{
					rv[targetIndex++] = new Position(x, maxRow);
				}
				maxRow -= 1;
				for(var y = maxRow; y >= minRow; --y)
				{
					rv[targetIndex++] = new Position(minCol, y);
				}
				minCol += 1;
			}
			return rv;
		}

		private static BitmapImage CreateFromStream(Stream stream)
		{
			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.StreamSource = stream;
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();
			bitmap.Freeze();
			return bitmap;
		}

		public static async Task<PuzzleEnvironment> Generate()
		{
			var pieces = await PuzzleProvider.LoadPieces();
			var bitmapImages = pieces.Select(
				p =>
				{
					using(var stream = ImageProvider.Load(p.ImageId))
					{
						return CreateFromStream(stream!)
;					}
				}
			);

			return new PuzzleEnvironment
			{
				PositionLookup = GeneratePositions().ToImmutableArray(),
				Images = bitmapImages.ToImmutableArray(),
				PieceSides = pieces.Select(p => p.Sides).Select(s => s.ToImmutableArray()).ToImmutableArray()
			};
		}
	}
}

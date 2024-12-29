﻿namespace Eternity
{
	using System.Collections.Immutable;

	public static class Sequence
	{
		const int NonCornerEdgePieceCount = 14 * 4;
		const int InnerPieceCount = 14 * 14;
		private static int[] GenerateDimensions()
		{
			int index = 0;
			var dimensions = new int[1 + (NonCornerEdgePieceCount - 1) + (InnerPieceCount - 1)];
			dimensions[index++] = 12;
			for(int i = NonCornerEdgePieceCount; i > 1; --i)
			{
				dimensions[index++] = i;
			}
			for(int i = InnerPieceCount; i > 1; --i)
			{
				dimensions[index++] = i;
			}
			return dimensions;
		}

		private static ImmutableArray<int> _dimensions = GenerateDimensions().ToImmutableArray();

		public static ImmutableArray<int> Dimensions => _dimensions;

		private static int[][] CornerPermutations = [
			[0, 1, 2, 3],
			[0, 1, 3, 2],
			[0, 2, 1, 3],
			[0, 2, 3, 1],
			[0, 3, 1, 2],
			[0, 3, 2, 1],
			[1, 0, 2, 3],
			[1, 0, 3, 2],
			[2, 0, 1, 3],
			[2, 0, 3, 1],
			[3, 0, 1, 2],
			[3, 0, 2, 1]
		];

		public static int[] GenerateRandomSequence()
		{
			var random = new Random();
			return _dimensions.Select(
				n => random.Next(n)
			).ToArray();
		}

		public static int[] GeneratePieceIndexes(IReadOnlyList<int> sequence)
		{
			var result = new int[256];
			var cornerPermutation = CornerPermutations[sequence[0]];
			int i = 0;
			for (; i < cornerPermutation.Length; ++i)
			{
				result[i] = cornerPermutation[i];
			}
			
			var usedEdgePieces = new bool[NonCornerEdgePieceCount];
			foreach(var unusedEdgePieceIndex in sequence.Skip(1).Take(NonCornerEdgePieceCount - 1))
			{
				var available = Enumerable.Range(0, usedEdgePieces.Length)
					.Where(n => !usedEdgePieces[n])
					.ElementAt(unusedEdgePieceIndex);
				result[i++] = available + 4;
				usedEdgePieces[available] = true;
			}
			for(int j = 0; j < usedEdgePieces.Length; ++j)
			{
				if (!usedEdgePieces[j])
				{
					result[i++] = j + 4;
					break;
				}
			}

			var usedInnerPieces = new bool[InnerPieceCount];
			foreach(var unusedInnerPieceIndex in sequence.Skip(1 + NonCornerEdgePieceCount - 1))
			{
				var available = Enumerable.Range(0, usedInnerPieces.Length)
					.Where(n => !usedInnerPieces[n])
					.ElementAt(unusedInnerPieceIndex);
				result[i++] = available + 4 + NonCornerEdgePieceCount;
				usedInnerPieces[available] = true;
			}
			for(int j = 0; j < usedInnerPieces.Length; ++j)
			{
				if(!usedInnerPieces[j])
				{
					result[i++] = j + 4 + NonCornerEdgePieceCount;
					break;
				}
			}

			return result;
		}
	}
}

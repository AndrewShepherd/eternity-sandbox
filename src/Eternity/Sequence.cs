namespace Eternity
{
	using System.Collections.Immutable;

	public static class Sequence
	{
		const int NonCornerEdgePieceCount = 14 * 4;
		const int InnerPieceCount = 14 * 14;

		private static int[][] CornerPermutations = [
			[0, 1, 2, 3],
			[0, 1, 3, 2],
			[0, 2, 1, 3],
			[0, 2, 3, 1],
			[0, 3, 1, 2],
			[0, 3, 2, 1],
			//[1, 0, 2, 3],
			//[1, 0, 3, 2],
			//[2, 0, 1, 3],
			//[2, 0, 3, 1],
			//[3, 0, 1, 2],
			//[3, 0, 2, 1]
		];
		private static int[] GenerateDimensions()
		{
			int index = 0;
			var dimensions = new int[1 + (NonCornerEdgePieceCount - 1) + (InnerPieceCount - 1)];
			dimensions[index++] = CornerPermutations.Length;
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


		public static IReadOnlyList<int> FirstSequence => new int[_dimensions.Length];

		public static IReadOnlyList<int> LastSequence = _dimensions.Select(d => d - 1).ToArray();
		public static int[] GenerateRandomSequence()
		{
			var random = new Random();
			return _dimensions.Select(
				n => random.Next(n)
			).ToArray();
		}

		public static IReadOnlyList<int> Increment(this IReadOnlyList<int> sequence, int index)
		{
			if (sequence[index] == _dimensions[index] - 1)
			{
				if (index == 0)
				{
					return Sequence.FirstSequence;
				}
				else
				{
					return Increment(sequence, index - 1);
				}
			}
			int[] result = new int[sequence.Count];
			for(int i = 0; i < sequence.Count; ++i)
			{
				result[i] = i switch
				{
					int n when n < index => sequence[i],
					int n when n == index => sequence[i] + 1,
					_ => 0
				};
			}
			return result;
		}

		public static IReadOnlyList<int> Decrement(this IReadOnlyList<int> sequence, int index)
		{
			if (sequence[index] == 0)
			{
				if (index == 0)
				{
					return Sequence.LastSequence;
				}
				else
				{
					return Decrement(sequence, index - 1);
				}
			}
			int[] result = new int[sequence.Count];
			for(int i = 0; i < sequence.Count; ++i)
			{
				result[i] = i switch
				{
					int n when n < index => sequence[i],
					int n when n == index => sequence[i] - 1,
					_ => _dimensions[i] - 1
				};
			}
			return result;
		}

		public static int ListPlacementIndexToSequenceIndex(int listPlacementIndex) =>
			listPlacementIndex switch
			{
				< 4 => 0,
				< 60 => listPlacementIndex - 3,
				_ => listPlacementIndex - 4
			};

		public static int[] SequenceIndexToPositionIndexes(int sequenceIndex) =>
			sequenceIndex switch
			{
				0 => [0, 1, 2, 3],
				> 0 and < 55 => [sequenceIndex + 3],
				55 => [sequenceIndex + 3, sequenceIndex + 4],
				_ => [sequenceIndex + 4]
			};
		public static IEnumerable<int> GeneratePieceIndexes(IReadOnlyList<int> sequence)
		{
			var cornerPermutation = CornerPermutations[sequence[0]];

			for (int i = 0; i < cornerPermutation.Length; ++i)
			{
				yield return cornerPermutation[i];
			}
			
			var usedEdgePieces = new bool[NonCornerEdgePieceCount];
			foreach(var unusedEdgePieceIndex in sequence.Skip(1).Take(NonCornerEdgePieceCount - 1))
			{
				var available = Enumerable.Range(0, usedEdgePieces.Length)
					.Where(n => !usedEdgePieces[n])
					.ElementAt(unusedEdgePieceIndex);
				yield return available + 4;
				usedEdgePieces[available] = true;
			}
			for(int j = 0; j < usedEdgePieces.Length; ++j)
			{
				if (!usedEdgePieces[j])
				{
					yield return j + 4;
					break;
				}
			}

			var innerPieces = Enumerable.Range(0, InnerPieceCount).ToList();
			foreach(var unusedInnerPieceIndex in sequence.Skip(1 + NonCornerEdgePieceCount - 1))
			{
				yield return innerPieces[unusedInnerPieceIndex] + 4 + NonCornerEdgePieceCount;
				innerPieces.RemoveAt(unusedInnerPieceIndex);
			}
			yield return innerPieces.First() + 4 + NonCornerEdgePieceCount;
		}
	}
}

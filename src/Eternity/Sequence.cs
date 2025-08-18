namespace Eternity;

public class SequenceSpecs(int pieceCount)
{
	public readonly int PieceCount = pieceCount;

	public readonly IReadOnlyList<int> Dimensions = Enumerable.Range(0, pieceCount).
		Select(i => pieceCount - i).ToArray();
}

public static class Sequence
{

	public static IReadOnlyList<int> GenerateFirst(
		this SequenceSpecs sequenceSpecs
	) => new int[sequenceSpecs.Dimensions.Count];

	public static IReadOnlyList<int> GenerateLast(
		this SequenceSpecs sequenceSpecs
	) => sequenceSpecs.Dimensions.Select(d => d - 1).ToArray();
	public static int[] GenerateRandomSequence(
		this SequenceSpecs sequenceSpecs
	)
	{
		var random = new Random();
		return sequenceSpecs.Dimensions.Select(
			n => random.Next(n)
		).ToArray();
	}

	public static IReadOnlyList<int> Increment(
		this SequenceSpecs sequenceSpecs,
		IReadOnlyList<int> sequence, 
		int index
	)
	{
		if (sequence[index] == sequenceSpecs.Dimensions[index] - 1)
		{
			if (index == 0)
			{
				return sequenceSpecs.GenerateFirst();
			}
			else
			{
				return sequenceSpecs.Increment(sequence, index - 1);
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

	public static IReadOnlyList<int> Decrement(
		this SequenceSpecs sequenceSpecs,
		IReadOnlyList<int> sequence,
		int index
	)
	{
		if (sequence[index] == 0)
		{
			if (index == 0)
			{
				return sequenceSpecs.GenerateLast();
			}
			else
			{
				return sequenceSpecs.Decrement(sequence, index - 1);
			}
		}
		int[] result = new int[sequence.Count];
		for(int i = 0; i < sequence.Count; ++i)
		{
			result[i] = i switch
			{
				int n when n < index => sequence[i],
				int n when n == index => sequence[i] - 1,
				_ => sequenceSpecs.Dimensions[i] - 1
			};
		}
		return result;
	}

	public static int ListPlacementIndexToSequenceIndex(
		int listPlacementIndex
	) => listPlacementIndex;

	public static IEnumerable<int> GeneratePieceIndexes(
		IReadOnlyList<int> sequence
	)
	{
		var pieceCount = sequence.Count;
		var used = new bool[pieceCount];
		for(int i = 0; i < sequence.Count; ++i)
		{
			var sequenceValue = sequence[i];
			for(int j = 0; j < used.Length; ++j)
			{
				if (!used[j])
				{
					if (sequenceValue == 0)
					{
						used[j] = true;
						yield return j;
						break;
					}
					else
					{
						sequenceValue -= 1;
					}
				}
			}
		}
		// The last remainig piece
		for(int i = 0; i < used.Length; ++i)
		{
			if (!used[i])
			{
				yield return i;
				break;
			}
		}
	}
}

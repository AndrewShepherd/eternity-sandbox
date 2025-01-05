namespace Eternity
{
	internal class SquareConstraintTransformQueue
	{
		public delegate SquareConstraint Transform(SquareConstraint before);

		private readonly List<Transform>[] _transforms = Enumerable.Range(0, 256)
			.Select(
				n => new List<Transform>()
			).ToArray();

		private readonly Queue<int> _toProcess = new();

		public void Push(int constraintIndex, Transform transform)
		{
			_transforms[constraintIndex].Add(transform);
			_toProcess.Enqueue(constraintIndex);
		}

		public Tuple<int, List<Transform>>? Pop()
		{
			while(_toProcess.TryDequeue(out int constraintIndex))
			{
				if (_transforms[constraintIndex].Any())
				{
					(
						var rv,
						_transforms[constraintIndex]
					) = (
						_transforms[constraintIndex],
						new List<Transform>()
					);
					return Tuple.Create(constraintIndex, rv);
				}
			}
			return null;
		}
	}
}

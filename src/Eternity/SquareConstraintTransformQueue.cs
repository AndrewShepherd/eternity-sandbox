namespace Eternity
{
	internal class SquareConstraintTransformQueue
	{
		private readonly List<SquareConstraint.TransformAction>[] _transforms = Enumerable.Range(0, 256)
			.Select(
				n => new List<SquareConstraint.TransformAction>()
			).ToArray();

		private readonly Queue<int> _toProcess = new();

		public void Push(int constraintIndex, SquareConstraint.TransformAction transform)
		{
			_transforms[constraintIndex].Add(transform);
			_toProcess.Enqueue(constraintIndex);
		}

		public bool HasItems => _toProcess.Count > 0;

		public Tuple<int, List<SquareConstraint.TransformAction>>? Pop()
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
						new List<SquareConstraint.TransformAction>()
					);
					return Tuple.Create(constraintIndex, rv);
				}
			}
			return null;
		}
	}
}

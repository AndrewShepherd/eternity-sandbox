namespace Eternity
{
	internal class SlotConstraintTransformQueue
	{
		private readonly List<SlotConstraint.TransformAction>[] _transforms = Enumerable.Range(0, 256)
			.Select(
				n => new List<SlotConstraint.TransformAction>()
			).ToArray();

		private readonly Queue<int> _toProcess = new();

		public void Push(int constraintIndex, SlotConstraint.TransformAction transform)
		{
			_transforms[constraintIndex].Add(transform);
			_toProcess.Enqueue(constraintIndex);
		}

		public bool HasItems => _toProcess.Count > 0;

		public Tuple<int, List<SlotConstraint.TransformAction>>? Pop()
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
						new List<SlotConstraint.TransformAction>()
					);
					return Tuple.Create(constraintIndex, rv);
				}
			}
			return null;
		}
	}
}

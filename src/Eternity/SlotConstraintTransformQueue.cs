using System.Collections.Immutable;

namespace Eternity
{
	using Lookup = ImmutableDictionary<Position, ImmutableList<SlotConstraint.TransformAction>>;
	internal class SlotConstraintTransformQueue
	{
		private Lookup _transforms = Lookup.Empty;

		private readonly Queue<Position> _toProcess = new();

		public void Push(Position position, SlotConstraint.TransformAction transform)
		{
			_transforms = _transforms.TryGetValue(position, out var transforms)
				? _transforms.SetItem(position, transforms.Add(transform))
				: _transforms.SetItem(position, ImmutableList<SlotConstraint.TransformAction>.Empty.Add(transform));
			_toProcess.Enqueue(position);
		}

		public bool HasItems => _toProcess.Count > 0;

		public Tuple<Position, IReadOnlyList<SlotConstraint.TransformAction>>? Pop()
		{
			while(_toProcess.TryDequeue(out Position? position))
			{
				if (_transforms.TryGetValue(position, out var l))
				{
					_transforms = _transforms.Remove(position);
					IReadOnlyList<SlotConstraint.TransformAction> rol = l;
					return Tuple.Create(position, rol);
				}
			}
			return null;
		}
	}
}

namespace Eternity;

using Lookup = Dictionary<Position, List<SlotConstraint.TransformAction>>;
internal class SlotConstraintTransformQueue
{
	private Lookup _transforms = new();

	private readonly Queue<Position> _toProcess = new();

	public void Push(Position position, SlotConstraint.TransformAction transform)
	{
		if (_transforms.TryGetValue(position, out var transforms))
		{
			transforms.Add(transform);
		}
		else
		{
			_transforms.Add(position, [transform]);
			_toProcess.Enqueue(position);
		}
	}

	public bool HasItems => _toProcess.Count > 0;

	public Tuple<Position, IReadOnlyList<SlotConstraint.TransformAction>>? Pop()
	{
		while(_toProcess.TryDequeue(out Position? position))
		{
			if (_transforms.TryGetValue(position, out var l))
			{
				_transforms.Remove(position);
				IReadOnlyList<SlotConstraint.TransformAction> rol = l;
				return Tuple.Create(position, rol);
			}
		}
		return null;
	}
}

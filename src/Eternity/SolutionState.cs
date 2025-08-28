namespace Eternity;

using System;
public sealed class SolutionState
{
	public TreeNode _treeNode = UnexploredTreeNode.Instance;

	public readonly IReadOnlyList<IReadOnlyList<ulong>> _pieceSides;

	public SolutionState(IReadOnlyList<IReadOnlyList<ulong>> pieceSides)
	{
		_pieceSides = pieceSides;
	}
}



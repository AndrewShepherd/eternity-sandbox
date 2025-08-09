namespace Eternity.WpfApp
{
	using Google.Protobuf.Collections;
	using System;
	using System.Collections.Immutable;
	internal sealed class SolutionState
	{
		public TreeNode _treeNode = UnexploredTreeNode.Instance;

		public readonly IReadOnlyList<ImmutableArray<int>> _pieceSides;


		public SolutionState(IReadOnlyList<ImmutableArray<int>> pieceSides)
		{
			_pieceSides = pieceSides;
		}
	}

	internal static class SolutionStateProto
	{
		public static Proto.RunningState Convert(SolutionState solutionState)
		{
			var runningState = new Eternity.Proto.RunningState()
			{
				Tree = ProtoTreeNodeConversions.Convert(solutionState._treeNode)
			};
			runningState.Pieces.AddRange(
				solutionState._pieceSides.Select(
					p => ProtoPieceConversions.ConvertPieceSides(p)
				)
			);
			return runningState;
		}

		private static FullyExploredTreeNode Convert(Proto.FullyExploredTreeNode fetn, IReadOnlyList<ImmutableArray<int>> pieces) =>
			new FullyExploredTreeNode
			{
				NodesExplored = ProtoConversions.Convert(fetn.NodesExplored),
				Solutions = fetn.Solutions.Select(
						s => ProtoConversions.Convert(s, pieces)
					).ToList(),
			};

		private static TreeNode BuildTree(Proto.TreeNode tree, IReadOnlyList<ImmutableArray<int>> pieces)
		{
			if (tree.InstanceCase == Proto.TreeNode.InstanceOneofCase.Unexplored)
			{
				return UnexploredTreeNode.Instance;
			}
			if (tree.InstanceCase == Proto.TreeNode.InstanceOneofCase.UnsuccessfulPlacement)
			{
				return UnsuccessfulPlacementTreeNode.Instance;
			}
			if (tree.InstanceCase == Proto.TreeNode.InstanceOneofCase.FullyExplored)
			{
				return Convert(tree.FullyExplored, pieces);
			}
			if (tree.InstanceCase == Proto.TreeNode.InstanceOneofCase.PartiallyExplored)
			{
				var treeNode = StackEntryExtensions.CreateInitialStack(
					StackEntryExtensions.ProgressForwards,
					pieces
				);
				if (treeNode is UnsuccessfulPlacementTreeNode)
				{
					return treeNode;
				}
				else if (treeNode is PartiallyExploredTreeNode petn)
				{
					for(int i = 0; i < tree.PartiallyExplored.ChildNodes.Count; ++i)
					{
						TreeNode newTreeNode = ExtendTreeNode(
							petn,
							tree.PartiallyExplored.ChildNodes[i],
							i,
							pieces
						);
						if (newTreeNode is PartiallyExploredTreeNode petn2)
						{
							petn = petn2;
						}
						else
						{
							return newTreeNode;
						}
					}
					return petn;
				}
				else
				{
					throw new Exception($"CreateInitialStack created unexpected tree node {treeNode}");
				}
			}

			throw new Exception($"tree.InstanceCase is an unexpected vaue of {tree.InstanceCase}");
		}

		private static TreeNode ExtendTreeNode(
			PartiallyExploredTreeNode petn,
			Proto.TreeNode protoChild,
			int index,
			IReadOnlyList<ImmutableArray<int>> pieces
		)
		{
			if (protoChild.InstanceCase == Proto.TreeNode.InstanceOneofCase.UnsuccessfulPlacement)
			{
				return petn.ReplaceAt(
					index,
					UnsuccessfulPlacementTreeNode.Instance
				);
			}
			else if (protoChild.InstanceCase == Proto.TreeNode.InstanceOneofCase.FullyExplored)
			{
				return petn.ReplaceAt(
					index,
					Convert(protoChild.FullyExplored, pieces)
				);
			}
			else if (protoChild.InstanceCase == Proto.TreeNode.InstanceOneofCase.PartiallyExplored)
			{
				var newTreeNode = StackEntryExtensions.GenerateChildNode(
					petn,
					index
				);
				if (newTreeNode is PartiallyExploredTreeNode petn2)
				{
					for (int i = 0; i < protoChild.PartiallyExplored.ChildNodes.Count; ++i)
					{
						newTreeNode = ExtendTreeNode(
							petn2,
							protoChild.PartiallyExplored.ChildNodes[i],
							i,
							pieces
						);
						if (newTreeNode is PartiallyExploredTreeNode petn3)
						{
							petn2 = petn3;
						}
						else
						{
							break;
						}
					}
				}
				return petn.ReplaceAt(
					index,
					newTreeNode
				);
			}
			else
			{
				return petn;
			}
		}

		internal static SolutionState Convert(Proto.RunningState runningStateProto)
		{
			var pieces = ProtoPieceConversions.ConvertProtoPieces(runningStateProto.Pieces);
			var solutionState = new SolutionState(pieces)
			{
				_treeNode = BuildTree(runningStateProto.Tree, pieces),
			};
			return solutionState;
		}
	}

}

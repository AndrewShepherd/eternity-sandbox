namespace Eternity
{
	using System.Collections.Immutable;
	using System.Numerics;

	public interface TreeNode
	{
		BigInteger NodesExplored { get; }
		BigInteger? TotalNodesEstimate { get; }

		IReadOnlyList<Placements> Solutions { get; }
	}

	public class FullyExploredTreeNode : TreeNode
	{
		public BigInteger NodesExplored { get; init; }

		public IReadOnlyList<Placements> Solutions { get; init; } = [];

		public BigInteger? TotalNodesEstimate => NodesExplored;
	}

	public class UnsuccessfulPlacementTreeNode :TreeNode
	{
		public BigInteger NodesExplored => 0;
		public BigInteger? TotalNodesEstimate => 0;

		public IReadOnlyList<Placements> Solutions => [];

		public static readonly TreeNode Instance = new UnsuccessfulPlacementTreeNode();
	}

	public class UnexploredTreeNode : TreeNode
	{
		public BigInteger NodesExplored => 0;
		public BigInteger? TotalNodesEstimate => null;

		public IReadOnlyList<Placements> Solutions => [];

		public static readonly TreeNode Instance = new UnexploredTreeNode();
	}

	public class PartiallyExploredTreeNode : TreeNode
	{
		public required ImmutableList<TreeNode> ChildNodes { get; init; }

		public required BigInteger NodesExplored { get; init; }
		public BigInteger? TotalNodesEstimate { get; init; } = null;

		public required StackEntry StackEntry { get; init; }

		public required IReadOnlyList<Placements> Solutions { get; init; }

		private static BigInteger? CalculateEstimate(BigInteger? accumulated, int contributors, int total)
		{
			if ((accumulated == null) || (contributors == 0))
			{
				return null;
			}
			else if (contributors == total)
			{
				return accumulated;
			}
			else
			{
				return accumulated.Value * total/ contributors;
			}
		}

		public TreeNode ReplaceAt(int index, TreeNode newNode)
		{
			var existing = ChildNodes.ElementAt(index);
			if (object.ReferenceEquals(existing, newNode))
			{
				return this;
			}
			var newChildren = ChildNodes.SetItem(index, newNode);
			// Now have to work out: 
			//   What kind of tree node should this be(FullyExplored, StackEntry)
			//   How many nodes explored are there
			//   What should the total nodes estimate be
			bool hasAtLeastOneStillToBeExplored = false;
			BigInteger accumulatedEstimate = 0;
			BigInteger accumulatedNodesExplored = 0;
			int estimateContributors = 0;
			IEnumerable<Placements> solutions = Enumerable.Empty<Placements>();
			foreach(var child in newChildren)
			{
				hasAtLeastOneStillToBeExplored = hasAtLeastOneStillToBeExplored || (child is PartiallyExploredTreeNode or UnexploredTreeNode);
				var thisEstimate = child.TotalNodesEstimate;
				if (thisEstimate.HasValue)
				{
					accumulatedEstimate += thisEstimate.Value;
					estimateContributors += 1;
				}
				accumulatedNodesExplored += child.NodesExplored;
				solutions = solutions.Concat(child.Solutions);
			}
			var calculatedEstimate = CalculateEstimate(accumulatedEstimate, estimateContributors, newChildren.Count);
			if (hasAtLeastOneStillToBeExplored)
			{
				return new PartiallyExploredTreeNode
				{
					ChildNodes = newChildren,
					NodesExplored = accumulatedNodesExplored + 1,
					StackEntry = this.StackEntry,
					TotalNodesEstimate = calculatedEstimate + 1,
					Solutions = solutions.ToList()
				};
			}
			else
			{
				return new FullyExploredTreeNode
				{
					NodesExplored = accumulatedNodesExplored + 1,
					Solutions = solutions.ToList()
				};
			}
		}
	}
}

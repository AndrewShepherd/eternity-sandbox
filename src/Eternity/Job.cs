using System.Collections.Immutable;

namespace Eternity
{
	public record class DividIntoJobsResult(
		TreeNode RootNode,
		ImmutableList<ImmutableList<int>> Paths
	);

	public static class Job
	{
		record class JobDivisionEntry(ImmutableList<int> Path, TreeNode TreeNode);

		private static IEnumerable<TreeNode> GetStack(TreeNode treeNode, IEnumerable<int> path)
		{
			yield return treeNode;
			foreach(int i in path)
			{
				treeNode = treeNode switch
				{
					PartiallyExploredTreeNode petn => petn.ChildNodes[i],
					_ => throw new Exception($"Expecting a partially explored tree node, got a {treeNode.GetType().Name}")
				};
				yield return treeNode;
			}
		}

		public static DividIntoJobsResult DivideIntoJobs(TreeNode rootNode, int requiredJobs)
		{
			if (rootNode is FullyExploredTreeNode or UnsuccessfulPlacementTreeNode)
			{
				return new(rootNode, []);
			}
			var queue = new Queue<JobDivisionEntry>();
			queue.Enqueue(new (ImmutableList<int>.Empty, rootNode));
			List<ImmutableList<int>> result = [];
			while (queue.Count < requiredJobs && queue.Any())
			{
				var (path, node) = queue.Dequeue();
				if (node is UnexploredTreeNode)
				{
					var stack = GetStack(rootNode, path).ToArray();
					var newNode = global::Eternity.StackEntryExtensions.GenerateChildNode(
						(PartiallyExploredTreeNode)stack[^2],
						path.Last()
					);
					var newChildNode = newNode;
					for(int i = stack.Length - 2; i >= 0; --i)
					{
						var nodeToChange = stack[i];
						var nodeToChangeChildIndex = path[i];
						if (nodeToChange is PartiallyExploredTreeNode p)
						{
							nodeToChange = p.ReplaceAt(nodeToChangeChildIndex, newChildNode);
						}
						newChildNode = nodeToChange;
					}
					rootNode = newChildNode;
					if (newNode is PartiallyExploredTreeNode p2)
					{
						for(int i = 0; i < p2.ChildNodes.Count; ++i)
						{
							queue.Enqueue(new JobDivisionEntry(
								path.Add(i),
								p2.ChildNodes[i]
							)
							);
						}
					}
				}

				if (node is PartiallyExploredTreeNode petn)
				{
					for (int i = 0; i < petn.ChildNodes.Count(); ++i)
					{
						var cn = petn.ChildNodes[i];
						if (cn is FullyExploredTreeNode or UnsuccessfulPlacementTreeNode)
						{
							continue;
						}
						queue.Enqueue(new(path.Add(i), cn));
						if (result.Count + queue.Count >= requiredJobs)
						{
							break;
						}
					}
				}
			}
			return new(
				rootNode,
				result.Take(requiredJobs)
				.Concat(
					queue.Take(requiredJobs - result.Count)
					.Select(r => r.Path)
				).ToImmutableList()
			);
		}
	}
}

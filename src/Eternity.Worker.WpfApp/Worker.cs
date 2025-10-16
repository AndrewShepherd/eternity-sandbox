namespace Eternity.Worker.WpfApp
{
	using System;
	using System.Reactive.Subjects;

	internal sealed class Worker()
	{
		private static (TreeNode, bool) Step(
			PartiallyExploredTreeNode treeNode,
			IEnumerable<int> initialPath
		)
		{
			int? childNodeIndex = initialPath.Any() ? initialPath.First() : null;
			if (childNodeIndex == null)
			{
				var candidates = Enumerable.Range(0, treeNode.ChildNodes.Count).Where(
					i => treeNode[i] is PartiallyExploredTreeNode or UnexploredTreeNode
				);
				if (candidates.Any())
				{
					childNodeIndex = candidates.First();
				}
			}
			if (childNodeIndex == null)
			{
				return (treeNode, false);
			}
			int index = childNodeIndex.Value;
			var childNode = treeNode[index];
				
			if (childNode is PartiallyExploredTreeNode petn)
			{
				var (replacementNode, childSuccess) = Step(petn, initialPath.Skip(1));
				if (childSuccess)
				{
					return (treeNode.ReplaceAt(index, replacementNode), true);
				}
				else
				{
					return (treeNode, false);
				}
			}
			else if (childNode is UnexploredTreeNode)
			{
				var newChild = StackEntryExtensions.GenerateChildNode(
					treeNode,
					index
				);
				return (treeNode.ReplaceAt(index, newChild), true);
			}
			else
			{
				return (treeNode, false);
			}
		}
		private static void DoWork(
			SolutionState solutionState,
			IEnumerable<int> initialPath,
			CancellationToken cancellationToken,
			IObserver<TreeNode> output
		)
		{
			try
			{
				if (!(solutionState._treeNode is PartiallyExploredTreeNode treeNode))
				{
					return;
				}
				while (!cancellationToken.IsCancellationRequested)
				{
					var (newNode, success) = Step(treeNode, initialPath);
					if (!success)
					{
						break;
					}
					output.OnNext(newNode);
					if (newNode is not PartiallyExploredTreeNode newPetn)
					{
						break;
					}
					treeNode = newPetn;
				}
			}
			finally
			{
				output.OnCompleted();
			}
		}


		public static IObservable<TreeNode> DoWork(
			SolutionState solutionState, 
			IEnumerable<int> initialPath, 
			CancellationToken cancellationToken
		)
		{
			var subject = new BehaviorSubject<TreeNode>(solutionState._treeNode);
			Task.Run(
				() => DoWork(solutionState, initialPath, cancellationToken, subject)
			);
			return subject;
		}
	}
}

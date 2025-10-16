namespace Eternity.Worker.WpfApp
{
	using System.Reactive.Subjects;

	internal sealed class Worker()
	{
		public static IObservable<TreeNode> DoWork(
			SolutionState solutionState, 
			IEnumerable<int> initialPath, 
			CancellationToken cancellationToken
		)
		{
			return new BehaviorSubject<TreeNode>(solutionState._treeNode);
		}
	}
}

namespace Eternity.Worker.WpfApp
{
	using System.Reactive.Linq;
	using System.Reactive.Subjects;
	using System.Threading;

	abstract record ConnectionStateEvent();

	sealed record TimerFiredEvent(int timerId) : ConnectionStateEvent;

	sealed record ServiceUnavailableEvent : ConnectionStateEvent;

	internal class ConnectionStateContext
	{
		System.Reactive.Subjects.Subject<ConnectionStateEvent> _events = new();
		public IObservable<ConnectionStateEvent> Events => _events;

		private int _timerId = 0;
		public int SetTimer(TimeSpan length)
		{
			int thisTimerId = Interlocked.Increment(ref _timerId);
			Task.Delay(length).ContinueWith(_ => FireEvent(new TimerFiredEvent(thisTimerId)));
			return thisTimerId;
		}

		public void FireEvent(ConnectionStateEvent e) => _events.OnNext(e);


		private readonly BehaviorSubject<IObservable<Placements>> _placementsSubject = new(
			Observable.Return(Eternity.Placements.None)
		);

		internal void SetPlacementsSource(IObservable<Placements> source) => _placementsSubject.OnNext(source);

		// This needs to go into Eterntiy lib
		private static Eternity.TreeNode GetMostAdvancedNode(Eternity.TreeNode node, IEnumerable<int> path)
		{
			if (node is Eternity.PartiallyExploredTreeNode petn)
			{
				var child = path.Any()
					? petn.ChildNodes[path.First()]
					: petn.ChildNodes.OfType<Eternity.PartiallyExploredTreeNode>().FirstOrDefault();
				if (child is Eternity.PartiallyExploredTreeNode)
				{
					return GetMostAdvancedNode(child, path.Skip(1));
				}
			}
			return node;
		}

		internal void SetWork(SolutionState solutionState, IEnumerable<int> initialPath)
		{
			if (_workerState is WorkerStateWorking wsw)
			{
				wsw.CancellationTokenSource.Cancel();
			}
			var cancellationTokenSource = new CancellationTokenSource();
			_workerState = new WorkerStateWorking(cancellationTokenSource);
			var treeNodes = Worker.DoWork(solutionState, initialPath, cancellationTokenSource.Token);
			var mostAdvancedNode = treeNodes.Select(
				tn => GetMostAdvancedNode(tn, initialPath)
			);
			var placements = mostAdvancedNode.Select(
				tn =>
					(tn as Eternity.PartiallyExploredTreeNode)
						?.StackEntry
						?.Placements
						?? Eternity.Placements.None
			);
			this.SetPlacementsSource(placements);
		}

		public IObservable<Placements> Placements => _placementsSubject.SelectMany(_ => _);

		private WorkerState _workerState = new WorkerStateIdle();
	}
}

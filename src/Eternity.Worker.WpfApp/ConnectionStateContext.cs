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
		Subject<ConnectionStateEvent> _events = new();
		Subject<IObservable<Eternity.Proto.MessageToServer>> _returnMessagesSubject = new();

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

		internal static Placements GetMostAdvancedPlacements(TreeNode treeNode, IEnumerable<int> initialPath)
			=>
				GetMostAdvancedNode(treeNode, initialPath) switch
				{
					PartiallyExploredTreeNode petn => petn.StackEntry.Placements ?? Eternity.Placements.None,
					_ => Eternity.Placements.None
				};

		internal IObservable<TreeNode> SetWork(SolutionState solutionState, IEnumerable<int> initialPath)
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
			return treeNodes;
		}

		internal void SetReturnMessages(IObservable<Eternity.Proto.MessageToServer> messageToSendBack)
		{
			throw new NotImplementedException();
		}

		public IObservable<Placements> Placements => _placementsSubject.SelectMany(_ => _);
		public IObservable<Proto.MessageToServer> MessagesToServer => _returnMessagesSubject
		.SelectMany(_ => _);

		private WorkerState _workerState = new WorkerStateIdle();
	}
}

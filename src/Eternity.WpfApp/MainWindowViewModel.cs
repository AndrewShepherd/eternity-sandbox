namespace Eternity.WpfApp
{
	using Prism.Commands;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Reactive.Subjects;
	using System.Reactive.Linq;
	using System.Windows.Input;
	using System.Reactive.Threading.Tasks;
	using System.Collections.Immutable;


	using static Eternity.Sequence;
	using System.Numerics;

	internal abstract class RunningState();

	internal sealed class Stopped() : RunningState;

	internal sealed class Running(CancellationTokenSource cancellationTokenSource) : RunningState
	{
		private readonly CancellationTokenSource _cancellationTokenSource = cancellationTokenSource;

		public CancellationTokenSource CancellationTokenSource => _cancellationTokenSource;
	}

	internal class MainWindowViewModel : INotifyPropertyChanged
	{
		RunningState _state = new Stopped();

		PropertyChangedEventHandler? _propertyChangedEventHandler;

		event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
		{
			add => _propertyChangedEventHandler += value;

			remove => _propertyChangedEventHandler -= value;
		}

		public IEnumerable<CanvasItem> CanvasItems
		{
			get;
			set;
		} = Enumerable.Empty<CanvasItem>();

		public Placements? Placements
		{
			get
			{
				return (this._solutionState?._treeNode) switch
				{
					StackEntryTreeNode seTreeNode => StackEntryExtensions.GetStackEntries(seTreeNode).Last().StackEntry.Placements,
					_ => default
				};
			}
		}

		public ICommand GenerateRandomCommand => new DelegateCommand(
			() => throw new NotImplementedException(),
			() => this.State is Stopped
		).ObservesProperty(() => this.State);

		public ICommand ResetSequenceCommand => new DelegateCommand(
			() =>
			{
				this._solutionState!._treeNode = StackEntryExtensions.CreateInitialStack(
					StackEntryExtensions.ProgressForwards,
					this._solutionState._pieceSides
				);
				this._rootTreeNode.OnNext(_solutionState!._treeNode);
			},
			() => this.State is Stopped
		).ObservesProperty(() => this.State);

		public ICommand StartCommand => new DelegateCommand(
			Start,
			() => CanStart
		).ObservesProperty(() => this.State);

		public ICommand GoBackwardsCommand => new DelegateCommand(
			GoBackwards,
			() => CanStart
		).ObservesProperty(() => this.State);


		public ICommand StopCommand => new DelegateCommand(
			Stop, 
			() => this.CanStop
		).ObservesProperty(() => this.State);

		public ICommand StepForwardCommand => new DelegateCommand(
			StepForward,
			() => _state is Stopped
		).ObservesProperty(() => this.State);

		public ICommand StepBackwardsCommand => new DelegateCommand(
			StepBackwards,
			() => _state is Stopped
		).ObservesProperty(() => this.State);

		public bool CanStart => _state is Stopped;

		public bool CanStop => _state is Running;

		void Step(Func<int?, int, int?> progressMethod)
		{
			if (_solutionState == null)
			{
				return;
			}
			_solutionState._treeNode = _solutionState._treeNode.Progress(
				progressMethod,
				_solutionState._pieceSides
			);
			this._rootTreeNode.OnNext(_solutionState._treeNode);
		}

		void StepForward() => Step(StackEntryExtensions.ProgressForwards);

		void StepBackwards() => Step(StackEntryExtensions.ProgressBackwards);

		private void LoopUntilAnswerFound(
			CancellationToken cancellationToken,
			Func<int?, int, int?> transform
		)
		{
			if (_solutionState == null)
			{
				return;
			}
			while (!cancellationToken.IsCancellationRequested)
			{
				if (false) // _solutionState._placementStack.Count == _solutionState._pieceSides.Count)
				{
					// This is a success!
					break;
				}
				Step(transform);
			}
			this.State = new Stopped();
		}

		public RunningState State
		{
			get => _state;
			set
			{
				if (this._state != value)
				{
					this._state = value;
					this._propertyChangedEventHandler?.Invoke(this, new(nameof(State)));
				}
			}
		}

		private void Go(Func<int?, int, int?> transform)
		{
			if (this._state is Stopped)
			{
				var tokenSource = new CancellationTokenSource();
				var thread = new Thread(
					new ThreadStart(
						() => LoopUntilAnswerFound(tokenSource.Token, transform)
					)
				);
				thread.Priority = ThreadPriority.Lowest;
				thread.Start();
				this.State = new Running(tokenSource);
			}

		}

		public void Start()
		{
			Go(StackEntryExtensions.ProgressForwards);
		}

		public void GoBackwards()
		{
			Go(StackEntryExtensions.ProgressBackwards);
		}

		public void Stop()
		{
			if (this._state is Running r)
			{
				r.CancellationTokenSource.Cancel();
			}
			this.State = new Stopped();
		}

		BehaviorSubject<IReadOnlyList<int>> _sequence = new BehaviorSubject<IReadOnlyList<int>>(
			new SequenceSpecs(256).GenerateFirst()
		);
		BehaviorSubject<TreeNode> _rootTreeNode = new BehaviorSubject<TreeNode>(new FullyExploredTreeNode { NodesExplored = 0 } );


		public IReadOnlyList<StackEntry> StackEntries
		{
			get
			{
				return _solutionState?._treeNode switch
				{
					StackEntryTreeNode tn => StackEntryExtensions.GetStackEntries(tn).Select(e => e.StackEntry).ToList(),
					_ => []
				};
			}
		}

		Task<PuzzleEnvironment> _generatePuzzleEnvironmentTask = PuzzleEnvironment.Generate();

		private int _selectedSequenceIndex = 0;
		public int SelectedSequenceIndex
		{
			get => _selectedSequenceIndex;
			set
			{
				if (_selectedSequenceIndex != value)
				{
					_selectedSequenceIndex = value;
					_propertyChangedEventHandler?.Invoke(this, new (nameof(SelectedSequenceIndex)));
				}
			}
		}

		private int _placementCount = 0;
		public int PlacementCount => _placementCount;

		public string ProgressText { get; set; }

		private SolutionState? _solutionState = null;

		public IReadOnlyList<int> Sequence => this._sequence.Value;

		private SequenceSpecs _sequenceSpecs = new SequenceSpecs(256);


		public void SetPieceSides(IReadOnlyList<ImmutableArray<int>> pieceSides)
		{
			_sequenceSpecs = new SequenceSpecs(pieceSides.Count);
			_solutionState = new SolutionState(pieceSides);
			_solutionState._treeNode = _solutionState._treeNode.Progress(
				StackEntryExtensions.ProgressForwards,
				_solutionState._pieceSides
			);
			this._rootTreeNode.OnNext(_solutionState._treeNode);
		}

		private async void SetInitialData()
		{
			var pieces = await PuzzleProvider.LoadPieces();
			var puzzleEnvironment = await _generatePuzzleEnvironmentTask;
			this.SetPieceSides(puzzleEnvironment.PieceSides);
		}

		private void SetUpObservables()
		{
			this.SelectedSequenceIndex = -1;

			var selectedSequenceIndexObservable = Observable.FromEventPattern<
				PropertyChangedEventHandler,
				PropertyChangedEventArgs
				>(
				handler => handler.Invoke,
				h => this._propertyChangedEventHandler += h,
				h => this._propertyChangedEventHandler -= h
			).Where(p => p.EventArgs.PropertyName == nameof(SelectedSequenceIndex))
			.Select(p => this._selectedSequenceIndex);


			var rootTreeNodeObservable = _rootTreeNode.Sample(TimeSpan.FromSeconds(0.5));

			var placementsObservable = rootTreeNodeObservable.Select(
				rootTreeNode => this.StackEntries.LastOrDefault(se => se.Placements != null)?.Placements
			);

			var scoreObservable = rootTreeNodeObservable.Select(
				CalculateStackProgress
			).Select(
				r => $"{r.division:N2} ({r.stepsTaken:N0}/{r.totalSteps:N0})"
			);

			scoreObservable.Subscribe(
				s =>
				{
					this.ProgressText = s;
					this._propChangedNotifier.PropertyChanged(nameof(this.ProgressText));
				}
			);

			rootTreeNodeObservable.Subscribe(
				se =>
				{
					this._propChangedNotifier.PropertyChanged(nameof(this.StackEntries));
				}
			);


			placementsObservable
				.Subscribe(
					placements =>
					{
						if (placements != null)
						{
							int placementCount = placements.Values.Where(p => p != null).Count();
							if (placementCount != this.PlacementCount)
							{
								this._placementCount = placementCount;
								this._propChangedNotifier.PropertyChanged(nameof(PlacementCount));
							}
						}
						this._propChangedNotifier.PropertyChanged(nameof(Placements));
					}
				);


			_sequence
				.Sample(TimeSpan.FromSeconds(0.1))
				//.ObserveOn(SynchronizationContext.Current!)
				.Subscribe(
					sequence =>
					{
						this._propChangedNotifier.PropertyChanged(nameof(Sequence));
					}
				);
		}

		record StackProgress(
			BigInteger stepsTaken,
			BigInteger totalSteps,
			double? division
		);

		private static StackProgress CalculateStackProgress(TreeNode treeNode) =>
			new StackProgress(
				treeNode.NodesExplored,
				treeNode.TotalNodesEstimate ?? 0,
				treeNode.NodesExplored switch 
				{ 
					BigInteger bi when (bi == 0) => default,
					BigInteger bi => (double)((treeNode.TotalNodesEstimate ?? 0)/treeNode.NodesExplored)
				}
			);

		internal void OnClosed()
		{
			if(this._state is Running r)
			{
				r.CancellationTokenSource.Cancel();
			}
		}

		readonly ThreadSafePropertyChangedNotifier _propChangedNotifier;


		public MainWindowViewModel()
		{
			_propChangedNotifier = new(
				args => this._propertyChangedEventHandler?.Invoke(this, args)
			);
			var puzzleEnvironmentObservable = _generatePuzzleEnvironmentTask.ToObservable();
			SetUpObservables();
			SetInitialData();
		}
	}
}

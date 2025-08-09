namespace Eternity.WpfApp
{
	using Prism.Commands;
	using ReactiveUI;
	using System.Collections.Generic;
	using System.Reactive.Subjects;
	using System.Reactive.Linq;
	using System.Windows.Input;
	using System.Reactive.Threading.Tasks;
	using System.Collections.Immutable;


	using static Eternity.Sequence;
	using System.Numerics;
	using System.IO;
	using Google.Protobuf;

	internal abstract class RunningState();

	internal sealed class Stopped() : RunningState;

	internal sealed class Running(CancellationTokenSource cancellationTokenSource) : RunningState
	{
		private readonly CancellationTokenSource _cancellationTokenSource = cancellationTokenSource;

		public CancellationTokenSource CancellationTokenSource => _cancellationTokenSource;
	}

	internal class MainWindowViewModel : ReactiveObject
	{
		RunningState _state = new Stopped();

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
					PartiallyExploredTreeNode seTreeNode => StackEntryExtensions.GetStackEntries(seTreeNode).Last().StackEntry.Placements,
					_ => default
				};
			}
		}

		public IReadOnlyList<Placements> Solutions => this._solutionState?._treeNode?.Solutions ?? [];

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
				Step(transform);
			}
			this.State = new Stopped();
		}

		public RunningState State
		{
			get => _state;
			set => this.RaiseAndSetIfChanged(ref _state, value);
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
					PartiallyExploredTreeNode tn => StackEntryExtensions.GetStackEntries(tn).Select(e => e.StackEntry).ToList(),
					_ => []
				};
			}
		}

		Task<PuzzleEnvironment> _generatePuzzleEnvironmentTask = PuzzleEnvironment.Generate();

		private int _selectedSequenceIndex = 0;
		public int SelectedSequenceIndex
		{
			get => _selectedSequenceIndex;
			set => this.RaiseAndSetIfChanged(ref _selectedSequenceIndex, value);
		}

		private int _placementCount = 0;
		public int PlacementCount => _placementCount;

		public string ProgressText { get; set; }

		private SolutionState? _solutionState = null;

		public IReadOnlyList<int> Sequence => this._sequence.Value;

		private SequenceSpecs _sequenceSpecs = new SequenceSpecs(256);


		public void SetPieceSides(IReadOnlyList<ImmutableArray<ulong>> pieceSides)
		{
			var solutionState = new SolutionState(pieceSides);
			solutionState._treeNode = solutionState._treeNode.Progress(
				StackEntryExtensions.ProgressForwards,
				solutionState._pieceSides
			);
			SetSolutionState(solutionState);
		}

		private void SetSolutionState(SolutionState newSolutionState)
		{
			_sequenceSpecs = new SequenceSpecs(newSolutionState._pieceSides.Count);
			_solutionState = newSolutionState;
			this._rootTreeNode.OnNext(_solutionState._treeNode);

		}

		private async void SetInitialData()
		{
			var pieces = await PuzzleProvider.LoadPieces();
			var puzzleEnvironment = await _generatePuzzleEnvironmentTask;
			this.SetPieceSides(puzzleEnvironment.PieceSides);
		}

		public BigInteger NodesProcessed { get; set; }
		public BigInteger EstimatedNodes { get; set; }

		private void SetUpObservables()
		{
			this.SelectedSequenceIndex = -1;

			var selectedSequenceIndexObservable = this.WhenAnyValue(vm => vm.SelectedSequenceIndex);

			var rootTreeNodeObservable = _rootTreeNode.Sample(TimeSpan.FromSeconds(0.5));

			var placementsObservable = rootTreeNodeObservable.Select(
				rootTreeNode => this.StackEntries.LastOrDefault(se => se.Placements != null)?.Placements
			);

			var scoreObservable = rootTreeNodeObservable.Select(
				CalculateStackProgress
			);

			scoreObservable.Subscribe(
				s =>
				{
					this.NodesProcessed = s.StepsTaken;
					this.EstimatedNodes = s.TotalSteps;
					this.RaisePropertyChanged(nameof(this.NodesProcessed));
					this.RaisePropertyChanged(nameof(this.EstimatedNodes));
				}
			);

			rootTreeNodeObservable.Subscribe(
				se =>
				{
					this.RaisePropertyChanged(nameof(this.StackEntries));
					this.RaisePropertyChanged(nameof(this.Solutions));
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
								this.RaisePropertyChanged(nameof(PlacementCount));
							}
						}
						this.RaisePropertyChanged(nameof(Placements));
					}
				);


			_sequence
				.Sample(TimeSpan.FromSeconds(0.1))
				//.ObserveOn(SynchronizationContext.Current!)
				.Subscribe(
					sequence =>
					{
						this.RaisePropertyChanged(nameof(Sequence));
					}
				);
		}

		record StackProgress(
			BigInteger StepsTaken,
			BigInteger TotalSteps,
			double? Division
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

		internal void SaveRunningState(Stream outputStream)
		{
			var solutionState = _solutionState;
			if (solutionState == null)
			{
				throw new Exception("No solution state to save");
			}
			var runningState = SolutionStateProto.Convert(solutionState);
			outputStream.Write(runningState.ToByteArray());
		}

		internal void LoadRunningState(Stream inputStream)
		{
			var runningStateProto = Proto.RunningState.Parser.ParseFrom(inputStream);
			SolutionState solutionState = SolutionStateProto.Convert(runningStateProto);
			this.SetSolutionState(solutionState);
		}

		public MainWindowViewModel()
		{
			var puzzleEnvironmentObservable = _generatePuzzleEnvironmentTask.ToObservable();
			SetUpObservables();
			SetInitialData();
		}
	}
}

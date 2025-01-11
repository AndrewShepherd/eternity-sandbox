﻿namespace Eternity.WpfApp
{
	using Prism.Commands;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Reactive.Subjects;
	using System.Reactive.Linq;
	using System.Windows.Input;
	using System.Reactive.Threading.Tasks;
	using System.IO;
	using System.Windows.Media.Imaging;
	using System.Collections.Immutable;


	using static Eternity.Sequence;

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
			get => _placements.Value;
			set
			{
				if(_placements.Value != value)
				{
					_placements.OnNext(value);
				}
			}
		}


		public ICommand GenerateRandomCommand => new DelegateCommand(
			() => this.SetSequence(GenerateRandomSequence()),
			() => this.State is Stopped
		).ObservesProperty(() => this.State);

		public ICommand ResetSequenceCommand => new DelegateCommand(
			() => this.SetSequence(FirstSequence),
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


		void Step(Func<IReadOnlyList<int>, int, IReadOnlyList<int>> transform)
		{
			if (_solutionState == null)
			{
				return;
			}
			var initialPlacements = this._placements.Value;
			var currentSequence = _sequence.Value;
			while (true)
			{
				int badIndex = _solutionState!.BadSequenceIndex;
				currentSequence = transform(currentSequence, badIndex);
				var placements = this._solutionState.SetSequence(currentSequence);
				if (!placements.Equals(initialPlacements))
				{
					this._sequence.OnNext(currentSequence);
					this._placements.OnNext(placements);
					break;
				}
			}
		}

		void StepForward() => Step((s, i) => s.Increment(i));

		void StepBackwards() => Step((s, i) => s.Decrement(i));

		private void LoopUntilAnswerFound(
			CancellationToken cancellationToken,
			Func<IReadOnlyList<int>, int, IReadOnlyList<int>> transform
		)
		{
			var currentSequence = _sequence.Value;
			var currentPlacements = this._placements.Value;
			while (!cancellationToken.IsCancellationRequested)
			{
				var badIndex = _solutionState!.BadSequenceIndex;
				if (badIndex >= currentSequence.Count - 1)
				{
					// This is a success!
					// We will never reach this code in a billion years
					// but it's nice to dream
					break;
				}
				currentSequence = transform(currentSequence, badIndex);
				var placements = _solutionState.SetSequence(currentSequence);

				_sequence.OnNext(currentSequence);

				if (placements != currentPlacements)
				{
					_placements.OnNext(placements);
					currentPlacements = placements;
				}
			}
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

		private void Go(Func<IReadOnlyList<int>, int, IReadOnlyList<int>> transform)
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
			Go(Eternity.Sequence.Increment);
		}

		public void GoBackwards()
		{
			Go(Eternity.Sequence.Decrement);
		}

		public void Stop()
		{
			if (this._state is Running r)
			{
				r.CancellationTokenSource.Cancel();
			}
			this.State = new Stopped();
		}

		private void SetSequence(IReadOnlyList<int> sequence)
		{
			if (_solutionState == null)
			{
				return;
			}
			var placements = _solutionState.SetSequence(sequence);
			this._sequence.OnNext(sequence);
			this._placements.OnNext(placements);

		}


		BehaviorSubject<IReadOnlyList<int>> _sequence = new BehaviorSubject<IReadOnlyList<int>>(FirstSequence);
		BehaviorSubject<Placements?> _placements = new BehaviorSubject<Placements?>(null);

		Task<PuzzleEnvironment> _generatePuzzleEnvironmentTask = PuzzleEnvironment.Generate();




		public string SequenceAsString { get; set; } = string.Empty;





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

		private SolutionState? _solutionState = null;

		public IReadOnlyList<int> Sequence => this._sequence.Value;

		private async void SetUpObservables()
		{
			var pieces = await PuzzleProvider.LoadPieces();



			var puzzleEnvironment = await _generatePuzzleEnvironmentTask;
			_solutionState = new SolutionState(puzzleEnvironment);

			var placements = Placements.CreateInitial(puzzleEnvironment.PieceSides);
			this._placements.OnNext(placements);
			var selectedSequenceIndexObservable = Observable.FromEventPattern<
				PropertyChangedEventHandler,
				PropertyChangedEventArgs
				>(
				handler => handler.Invoke,
				h => this._propertyChangedEventHandler += h,
				h => this._propertyChangedEventHandler -= h
			).Where(p => p.EventArgs.PropertyName == nameof(SelectedSequenceIndex))
			.Select(p => this._selectedSequenceIndex);

			var placementsObservable = _placements.Sample(TimeSpan.FromSeconds(0.5));


			placementsObservable
				.ObserveOn(SynchronizationContext.Current!)
				.Subscribe(
					placements =>
					{
						if (placements != null)
						{
							int placementCount = placements.Values.Where(p => p != null).Count();
							if (placementCount != this.PlacementCount)
							{
								this._placementCount = placementCount;
								this._propertyChangedEventHandler?.Invoke(this, new(nameof(PlacementCount)));
							}
						}
						this._propertyChangedEventHandler?.Invoke(this, new(nameof(this.Placements)));
					}
				);

			this.SelectedSequenceIndex = -1;
			_sequence
				.Sample(TimeSpan.FromSeconds(0.1))
				.ObserveOn(SynchronizationContext.Current!)
				.Subscribe(
					sequence =>
					{
						this.SequenceAsString = SequenceListEntry.SequenceToString(sequence);

						this._propertyChangedEventHandler?.Invoke(this, new(nameof(SequenceAsString)));
						this._propertyChangedEventHandler?.Invoke(this, new(nameof(Sequence)));
					}
				);
		}

		internal void OnClosed()
		{
			if(this._state is Running r)
			{
				r.CancellationTokenSource.Cancel();
			}
		}

		public MainWindowViewModel()
		{
			var puzzleEnvironmentObservable = _generatePuzzleEnvironmentTask.ToObservable();
			SetUpObservables();
			this.SetSequence(FirstSequence);
		}
	}
}

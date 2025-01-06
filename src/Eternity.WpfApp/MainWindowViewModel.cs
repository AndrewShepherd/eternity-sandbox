namespace Eternity.WpfApp
{
	using Prism.Commands;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Reactive.Subjects;
	using System.Reactive.Linq;
	using System.Windows.Input;
	using static Eternity.WpfApp.CanvasItemExtensions;
	using System.Reactive.Threading.Tasks;
	using System.Diagnostics;
	using System.IO;
	using System.Windows.Media.Imaging;
	using System.Collections.Immutable;
	using System.Collections.ObjectModel;


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


		public ICommand GenerateRandomCommand => new DelegateCommand(
			() => this.SetSequence(Sequence.GenerateRandomSequence()),
			() => this.State is Stopped
		).ObservesProperty(() => this.State);

		public ICommand ResetSequenceCommand => new DelegateCommand(
			() => this.SetSequence(Sequence.FirstSequence),
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


		async Task Step(Func<IReadOnlyList<int>, int, IReadOnlyList<int>> transform)
		{
			var currentSequence = _sequence.Value;
			var puzzleEnvironment = await _generatePuzzleEnvironmentTask;
			var placementStack = new PlacementStack();
			var (initialPlacementCount, initialPlacements) = placementStack.ApplyPieceOrder(
				puzzleEnvironment,
				Sequence.GeneratePieceIndexes(currentSequence)
			);
			int initialBadSequenceIndex = Sequence.ListPlacementIndexToSequenceIndex(
				initialPlacementCount
			);
			int placementCount = initialPlacementCount;

			while (true)
			{
				int badIndex = Sequence.ListPlacementIndexToSequenceIndex(placementCount);
				currentSequence = transform(currentSequence, badIndex);
				(placementCount, var placements) = placementStack.ApplyPieceOrder(
					puzzleEnvironment,
					Sequence.GeneratePieceIndexes(currentSequence)
				);
				if (!placements.Equals(initialPlacements))
				{
					this._sequence.OnNext(currentSequence);
					this._placements.OnNext(placements);
					break;
				}
			}

		}

		async void StepForward() => await Step((s, i) => s.Increment(i));

		async void StepBackwards() => await Step((s, i) => s.Decrement(i));

		private void LoopUntilAnswerFound(
			CancellationToken cancellationToken,
			Func<IReadOnlyList<int>, int, IReadOnlyList<int>> transform
		)
		{
			var currentSequence = _sequence.Value;
			var puzzleEnvironment = _generatePuzzleEnvironmentTask.Result;

			var placementStack = new PlacementStack();

			while (!cancellationToken.IsCancellationRequested)
			{
				var (placementCount, placements) = placementStack.ApplyPieceOrder(
					puzzleEnvironment,
					Sequence.GeneratePieceIndexes(currentSequence)
				);

				_sequence.OnNext(currentSequence);
				_placements.OnNext(placements);

				if (placementCount == placementStack._stackEntries.Length)
				{
					// This is a success!
					// We will never reach this code in a billion years
					// but it's nice to dream
					break;
				}
				int badIndex = Sequence.ListPlacementIndexToSequenceIndex(placementCount);
				currentSequence = transform(currentSequence, badIndex);
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
			Go(Sequence.Increment);
		}

		public void GoBackwards()
		{
			Go(Sequence.Decrement);
		}

		public void Stop()
		{
			if (this._state is Running r)
			{
				r.CancellationTokenSource.Cancel();
			}
			this.State = new Stopped();
		}

		private async void SetSequence(IReadOnlyList<int> sequence)
		{
			var puzzleEnvironment = await _generatePuzzleEnvironmentTask;
			var placementStack = new PlacementStack();
			var (count, placements) = placementStack.ApplyPieceOrder(
				puzzleEnvironment,
				Sequence.GeneratePieceIndexes(sequence)
			);
			this._sequence.OnNext(sequence);
			this._placements.OnNext(placements);

		}

		private void GenerateRandom()
		{
			this.SetSequence(Sequence.GenerateRandomSequence());
		}


		BehaviorSubject<IReadOnlyList<int>> _sequence = new BehaviorSubject<IReadOnlyList<int>>(Sequence.FirstSequence);
		BehaviorSubject<Placements?> _placements = new BehaviorSubject<Placements?>(null);

		Task<PuzzleEnvironment> _generatePuzzleEnvironmentTask = PuzzleEnvironment.Generate();




		public string SequenceAsString { get; set; } = string.Empty;


		private static BitmapImage CreateFromStream(Stream stream)
		{
			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.StreamSource = stream;
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();
			bitmap.Freeze();
			return bitmap;
		}

		public ObservableCollection<SequenceListEntry> SequenceListEntries { get; set; }

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

		private IEnumerable<CanvasItem> GenerateCanvasItems(
			double bitmapWidth,
			double bitmapHeight,
			ImmutableList<BitmapImage> bitmapImages,
			Placements placements,
			int selectedSequenceIndex)
		{
			var pieceItems = CanvasItemExtensions.GenerateCanvasPieceItems(
				bitmapImages,
				placements.Values
			);

			var constraintItems = CanvasItemExtensions.GenerateCanvasConstraintItem(
				bitmapWidth,
				bitmapHeight,
				placements.Constraints
			);

			var canvasItems = pieceItems.Cast<CanvasItem>().Concat(constraintItems).ToList();

			if (selectedSequenceIndex >= 0)
			{
				var highlightedPositionIndexes = Sequence.SequenceIndexToPositionIndexes(SelectedSequenceIndex);
				var highlightedPositions = highlightedPositionIndexes
					.Select(i => Positions.PositionLookup[i])
					.ToArray();
				foreach (var position in highlightedPositions)
				{
					canvasItems.Add(
						new CanvasHighlightItem
						{
							Top = position.Y * bitmapHeight,
							Left = position.X * bitmapWidth,
							Width = bitmapWidth,
							Height = bitmapHeight,
						}
					);
				}
			}
			return canvasItems;
		}

		private int _placementCount = 0;
		public int PlacementCount => _placementCount;

		private async void SetUpObservables()
		{
			var pieces = await PuzzleProvider.LoadPieces();
			var bitmapImages = pieces.Select(
				p =>
				{
					using (var stream = ImageProvider.Load(p.ImageId))
						return CreateFromStream(stream!);
				}
			).ToImmutableList();
			var bitmapWidth = bitmapImages[0].Width;
			var bitmapHeight = bitmapImages[0].Height;

			var puzzleEnvironment = await _generatePuzzleEnvironmentTask;

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
					}
				);
			var canvasItemsObservable = Observable.CombineLatest(
				placementsObservable,
				selectedSequenceIndexObservable,
				(listPlacements, selectedSequenceIndex) => Tuple.Create(listPlacements, selectedSequenceIndex)
			).Select(
				t =>
				{
					return GenerateCanvasItems(
						bitmapWidth,
						bitmapHeight,
						bitmapImages,
						t.Item1,
						t.Item2
					);
				}
			);

			canvasItemsObservable
				.ObserveOn(SynchronizationContext.Current!)
				.Subscribe(
					t =>
					{
						this.CanvasItems = t;
						this._propertyChangedEventHandler?.Invoke(this, new(nameof(CanvasItems)));
					}
				);
			this.SelectedSequenceIndex = -1;
			_sequence
				.Sample(TimeSpan.FromSeconds(0.1))
				.ObserveOn(SynchronizationContext.Current!)
				.Subscribe(
					sequence =>
					{
						for (int i = 0; i < sequence.Count; i++)
						{
							this.SequenceListEntries[i].Value = sequence[i];
						}
						this.SequenceAsString = SequenceListEntry.SequenceToString(sequence);

						this._propertyChangedEventHandler?.Invoke(this, new(nameof(SequenceAsString)));
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
			this.SequenceListEntries = new ObservableCollection<SequenceListEntry>(
				Sequence.Dimensions.Select(d => new SequenceListEntry { Value = 0 })
			);
			SetUpObservables();
			this.SetSequence(Sequence.FirstSequence);
		}
	}
}

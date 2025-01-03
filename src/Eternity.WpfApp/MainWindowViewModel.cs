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

	internal class MainWindowViewModel : INotifyPropertyChanged
	{
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


		public ICommand GenerateRandomCommand => new DelegateCommand(GenerateRandom);

		public ICommand TryNextCommand => new DelegateCommand(TryNext);


		private void LoopUntilAnswerFound(CancellationToken cancellationToken)
		{
			var currentSequence = _sequence.Value;
			var puzzleEnvironment = _generatePuzzleEnvironmentTask.Result;

			var placementStack = new PlacementStack();

			while (!cancellationToken.IsCancellationRequested)
			{
				IEnumerable<int> pieceIndexes = Sequence.GeneratePieceIndexes(currentSequence);

				var (placementCount, placements) = placementStack.ApplyPieceOrder(
					puzzleEnvironment,
					pieceIndexes
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
				currentSequence = Sequence.Increment(currentSequence, badIndex);
			}
		}

		CancellationTokenSource _loopCancellationToken = new CancellationTokenSource();
		public void TryNext()
		{
			var thread = new Thread(
				new ThreadStart(
					() => LoopUntilAnswerFound(_loopCancellationToken.Token)
				)
			);
			thread.Priority = ThreadPriority.Lowest;
			thread.Start();
		}

		private void GenerateRandom()
		{
			this._sequence.OnNext(Sequence.GenerateRandomSequence());
		}


		BehaviorSubject<int[]> _sequence = new BehaviorSubject<int[]>(Sequence.FirstSequence);
		BehaviorSubject<Placements> _placements = new BehaviorSubject<Placements>(Placements.Empty);

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
			PuzzleEnvironment puzzleEnvironment,
			double bitmapWidth,
			double bitmapHeight,
			ImmutableList<BitmapImage> bitmapImages,
			IReadOnlyList<Placement?> listPlacements,
			int selectedSequenceIndex)
		{
			var canvasItems = CanvasItemExtensions.GenerateCanvasItems(
				puzzleEnvironment,
				bitmapImages,
				listPlacements
			).Cast<CanvasItem>().ToList();

			if (selectedSequenceIndex >= 0)
			{
				var highlightedPositionIndexes = Sequence.SequenceIndexToPositionIndexes(SelectedSequenceIndex);
				var highlightedPositions = highlightedPositionIndexes
					.Select(i => puzzleEnvironment.PositionLookup[i])
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


			placementsObservable.Subscribe(
				placements =>
				{
					int placementCount = placements.Values.Where(p => p != null).Count();
					if (placementCount != this.PlacementCount)
					{
						this._placementCount = placementCount;
						this._propertyChangedEventHandler?.Invoke(this, new(nameof(PlacementCount)));
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
					return GenerateCanvasItems(puzzleEnvironment,
							bitmapWidth,
							bitmapHeight,
							bitmapImages,
							t.Item1.Values,
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
						for (int i = 0; i < sequence.Length; i++)
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
			this._loopCancellationToken.Cancel();
		}

		public MainWindowViewModel()
		{
			var puzzleEnvironmentObservable = _generatePuzzleEnvironmentTask.ToObservable();
			this.SequenceListEntries = new ObservableCollection<SequenceListEntry>(
				Sequence.Dimensions.Select(d => new SequenceListEntry { Value = 0 })
			);
			SetUpObservables();

		}
	}
}

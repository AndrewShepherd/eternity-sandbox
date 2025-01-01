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


		private record class StackEntry(int PieceIndex, Placements Placements);

		private void LoopUntilAnswerFound(CancellationToken cancellationToken)
		{
			var currentSequence = _sequence.Value;
			var puzzleEnvironment = _generatePuzzleEnvironmentTask.Result;

			var stackEntries = new StackEntry?[256];
			while (!cancellationToken.IsCancellationRequested)
			{
				IEnumerable<int> pieceIndexes = Sequence.GeneratePieceIndexes(currentSequence);

				// Find the stack entry that matches this
				var matchingStackEntryIndex = -1;
				var matchingPlacements = Placements.Empty;

				var pieceIndexEnumerator = pieceIndexes.GetEnumerator();

				for (int i = 0; i < stackEntries.Length; ++i)
				{
					pieceIndexEnumerator.MoveNext();
					var thisEntry = stackEntries[i];
					if (thisEntry == null)
					{
						break;
					}

					if (thisEntry.PieceIndex != pieceIndexEnumerator.Current)
					{
						for (int j = 0; j < stackEntries.Length; ++j)
						{
							if (stackEntries[j] == null)
							{
								break;
							}
							stackEntries[j] = null;
						}
						break;
					}
					matchingStackEntryIndex = i;
					matchingPlacements = thisEntry.Placements;
				}
				int? badListPlacementIndex = default;
				for (int i = matchingStackEntryIndex + 1; true; ++i)
				{
					var newPlacements = TryAddPiece(
						puzzleEnvironment,
						matchingPlacements,
						i,
						pieceIndexEnumerator.Current
					);
					if (newPlacements == null)
					{
						badListPlacementIndex = i;
						break;
					}
					stackEntries[i] = new StackEntry(pieceIndexEnumerator.Current, newPlacements);
					matchingPlacements = newPlacements;
					if (!pieceIndexEnumerator.MoveNext())
					{
						break;
					}
				}
				if (!badListPlacementIndex.HasValue)
				{
					break;
				}
				int badIndex = Sequence.ListPlacementIndexToSequenceIndex(badListPlacementIndex.Value);
				currentSequence = Sequence.Increment(currentSequence, badIndex);
				_sequence.OnNext(currentSequence);
				_placements.OnNext(matchingPlacements);
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

		private static Placements? TryAddPiece(
			PuzzleEnvironment puzzleEnvironment,
			Placements listPlacements,
			int positionIndex,
			int pieceIndex
			)
		{
			var pieceIndexAlredyThere = listPlacements.Values[positionIndex]?.PieceIndex;
			if (pieceIndexAlredyThere != null)
			{
				if (pieceIndexAlredyThere == pieceIndex)
				{
					return listPlacements;
				}
				else
				{
					return null;
				}
			}
			if (listPlacements.ContainsPieceIndex(pieceIndex))
			{
				return null;
			}

			var rotations = PuzzleSolver.GetPossibleRotations(
				puzzleEnvironment,
				positionIndex,
				pieceIndex,
				listPlacements.Values
			);

			if (rotations.Length == 0)
			{
				return null;
			}


			listPlacements = listPlacements.SetItem(positionIndex, new Placement(pieceIndex, rotations));
	
			// There may be existing placements which had multiple rotations
			// as a result of placing this piece they may no longer have multiple
			// rotations
			var adjacentPlacementIndexes = PuzzleSolver.GetAdjacentPlacementIndexes(puzzleEnvironment, positionIndex)
				.Where(
					pi => listPlacements.Values[pi] != null 
					&& listPlacements.Values[pi]!.Rotations.Length > 1
				).ToArray();
			if (adjacentPlacementIndexes.Length > 0)
			{
				foreach (var adjacentPlacementIndex in adjacentPlacementIndexes)
				{
					var thisPlacement = listPlacements.Values[adjacentPlacementIndex];
					if (thisPlacement == null)
					{
						throw new Exception("This cannot have happend");
					}
					var thisRotations = PuzzleSolver.GetPossibleRotations(
						puzzleEnvironment,
						adjacentPlacementIndex,
						thisPlacement.PieceIndex,
						listPlacements.Values
					);
					if (thisRotations.Length == 0)
					{
						throw new Exception("After placing a piece, an existing piece was in an illegal state");
					}
					if (thisRotations.Length < thisPlacement.Rotations.Length)
					{
						listPlacements = listPlacements.SetItem(
							adjacentPlacementIndex,
							new (
								thisPlacement.PieceIndex,
								thisPlacement.Rotations
							)
						);
					}
				}
			}

			// Perform a sweep. Get a list of all of the positions
			// that are blank but have adjacent non-blank
			bool mustPerformSweep = true;
			while(mustPerformSweep)
			{
				mustPerformSweep = false;
				List<int> blanksWithAdjacentFills = [];
				for (int posIndex = 0; posIndex < listPlacements.Values.Count; ++posIndex)
				{
					if (listPlacements.Values[posIndex] == null)
					{
						if (
							PuzzleSolver.GetAdjacentPlacementIndexes(puzzleEnvironment, posIndex)
								.Where(adj => listPlacements.Values[adj] != null)
								.Any()
						)
						{
							blanksWithAdjacentFills.Add(posIndex);
						}
					}
				}
				foreach(int blankPosIndex in blanksWithAdjacentFills)
				{
					var edgeRequirements = PuzzleSolver.GetEdgeRequirements(
						puzzleEnvironment,
						listPlacements.Values,
						blankPosIndex
					);
					// Work out, among all of the remaining pieces,
					// if there iz zero, one, or more than one possible 
					// choices for this position
					Placement? possiblePlacement = null;
					bool moreThanOne = false;
					for(int candidatePieceIndex = 0; candidatePieceIndex < 256; ++candidatePieceIndex)
					{
						if (listPlacements.ContainsPieceIndex(candidatePieceIndex))
						{
							continue;
						}
						var candidateEdges = puzzleEnvironment.PieceSides[candidatePieceIndex];
						var possibleRotations = edgeRequirements.SelectMany(
							er => PuzzleSolver.GetRotations(candidateEdges, er)
						).ToArray();
						if (possibleRotations.Any())
						{
							if (possiblePlacement == null)
							{
								possiblePlacement = new Placement(candidatePieceIndex, possibleRotations);
							}
							else
							{
								moreThanOne = true;
								break;
							}
						}
					}
					if(moreThanOne)
					{
						continue;
					}
					if (possiblePlacement == null)
					{
						return null;
					}
					listPlacements = listPlacements.SetItem(blankPosIndex, possiblePlacement);
					mustPerformSweep = true;
				}
			}
			return listPlacements;
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

			var canvasItemsObservable = Observable.CombineLatest(
				placementsObservable,
				selectedSequenceIndexObservable,
				(listPlacements, selectedSequenceIndex) => Tuple.Create(listPlacements, selectedSequenceIndex)
			).Select(
				t =>
				GenerateCanvasItems(puzzleEnvironment,
							bitmapWidth,
							bitmapHeight,
							bitmapImages,
							t.Item1.Values,
							t.Item2
						)
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

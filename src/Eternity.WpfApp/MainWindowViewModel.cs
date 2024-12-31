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
	using System.Reactive.Concurrency;
	using System.Collections.ObjectModel;
	using System.IO;
	using System.Windows.Media.Imaging;
	using System.Collections.Immutable;

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


		private record class StackEntry(int PieceIndex, Placement?[] Placements);

		private void LoopUntilAnswerFound()
		{
			var currentSequence = _sequence.Value;
			var puzzleEnvironment = _generatePuzzleEnvironmentTask.Result;

			var stackEntries = new StackEntry?[256];
			var noPlacements = new Placement?[256];
			while (true)
			{
				IEnumerable<int> pieceIndexes = Sequence.GeneratePieceIndexes(currentSequence);

				// Find the stack entry that matches this
				var matchingStackEntryIndex = -1;
				var matchingPlacements = noPlacements;

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

		public void TryNext()
		{
			var thread = new System.Threading.Thread(new ThreadStart(
				LoopUntilAnswerFound
				)
			);
			thread.Priority = ThreadPriority.Lowest;
			thread.Start();
		}

		private static Placement?[]? TryAddPiece(
			PuzzleEnvironment puzzleEnvironment,
			Placement?[] existingPlacements,
			int positionIndex,
			int pieceIndex
			)
		{
			var pieceIndexAlredyThere = existingPlacements[positionIndex]?.PieceIndex;
			if (pieceIndexAlredyThere != null)
			{
				if (pieceIndexAlredyThere == pieceIndex)
				{
					return existingPlacements;
				}
				else
				{
					return null;
				}
			}
			bool[] placedPieceIndexes = new bool[256];
			foreach(var p in existingPlacements)
			{
				if (p != null)
				{
					placedPieceIndexes[p.PieceIndex] = true;
				}
			}
			if (placedPieceIndexes[pieceIndex])
			{
				return null;
			}

			var listPlacements = new Placement?[existingPlacements.Length];
			existingPlacements.CopyTo(listPlacements, 0);
				
			var rotations = PuzzleSolver.GetPossibleRotations(
				puzzleEnvironment,
				positionIndex,
				pieceIndex,
				listPlacements
			);
			if (rotations.Length == 0)
			{
				return null;
			}
			listPlacements[positionIndex] = new Placement(pieceIndex, rotations);
			placedPieceIndexes[pieceIndex] = true;

			// There may be existing placements which had multiple rotations
			// as a result of placing this piece they may no longer have multiple
			// rotations
			var adjacentPlacementIndexes = PuzzleSolver.GetAdjacentPlacementIndexes(puzzleEnvironment, positionIndex)
				.Where(pi => listPlacements[pi] != null && listPlacements[pi]!.Rotations.Length > 1)
				.ToArray();
			if (adjacentPlacementIndexes.Length > 0)
			{
				foreach (var adjacentPlacementIndex in adjacentPlacementIndexes)
				{
					var thisPlacement = listPlacements[adjacentPlacementIndex];
					if (thisPlacement == null)
					{
						throw new Exception("This cannot have happend");
					}
					var thisRotations = PuzzleSolver.GetPossibleRotations(
						puzzleEnvironment,
						adjacentPlacementIndex,
						thisPlacement.PieceIndex,
						listPlacements
					);
					if (thisRotations.Length == 0)
					{
						throw new Exception("After placing a piece, an existing piece was in an illegal state");
					}
					if (thisRotations.Length < thisPlacement.Rotations.Length)
					{
						listPlacements[adjacentPlacementIndex] = new Placement(
							thisPlacement.PieceIndex,
							thisRotations
						);
						placedPieceIndexes[thisPlacement.PieceIndex] = true;
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
				for (int posIndex = 0; posIndex < listPlacements.Length; ++posIndex)
				{
					if (listPlacements[posIndex] == null)
					{
						if (
							PuzzleSolver.GetAdjacentPlacementIndexes(puzzleEnvironment, posIndex)
								.Where(adj => listPlacements[adj] != null)
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
						listPlacements,
						blankPosIndex
					);
					// Work out, among all of the remaining pieces,
					// if there iz zero, one, or more than one possible 
					// choices for this position
					Placement? possiblePlacement = null;
					bool moreThanOne = false;
					for(int candidatePieceIndex = 0; candidatePieceIndex < 256; ++candidatePieceIndex)
					{
						if (placedPieceIndexes[candidatePieceIndex])
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
					listPlacements[blankPosIndex] = possiblePlacement;
					placedPieceIndexes[possiblePlacement.PieceIndex] = true;
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
		BehaviorSubject<Placement?[]> _placements = new BehaviorSubject<Placement?[]>([]);

		Task<PuzzleEnvironment> _generatePuzzleEnvironmentTask = PuzzleEnvironment.Generate();


		private static string AsTwoDigitHex(int n)
		{
			var unpaddedHex = $"{n:X}";
			return unpaddedHex.Length == 1 ? $"0{unpaddedHex}" : unpaddedHex;
		}
		private static string SequenceToString(IEnumerable<int> sequence) =>
			string.Join(' ', sequence.Select(AsTwoDigitHex));

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

			var puzzleEnvironment = await _generatePuzzleEnvironmentTask;
			_placements.Sample(TimeSpan.FromSeconds(0.25))
				.Subscribe(
					listPlacements =>
					{
						var canvasItems = GenerateCanvasItems(
							puzzleEnvironment,
							bitmapImages,
							listPlacements
						).ToArray();
						this.CanvasItems = canvasItems;
						this._propertyChangedEventHandler?.Invoke(this, new(nameof(CanvasItems)));
						Debug.WriteLine($"{DateTime.Now.ToLongTimeString()} - converting sequence to canvas items. Item Count: {canvasItems.Length}");
					}
				);
			_sequence
				.Sample(TimeSpan.FromSeconds(0.1))
				.Subscribe(
					sequence =>
					{
						this.SequenceAsString = SequenceToString(sequence);
						this._propertyChangedEventHandler?.Invoke(this, new(nameof(SequenceAsString)));
					}
				);
		}

		public MainWindowViewModel()
		{
			var puzzleEnvironmentObservable = _generatePuzzleEnvironmentTask.ToObservable();

			SetUpObservables();

		}
	}
}

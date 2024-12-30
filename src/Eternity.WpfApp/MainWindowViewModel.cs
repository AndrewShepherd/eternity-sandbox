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


		public void TryNext()
		{
			var t = Task.Run(
				async () =>
				{
					var currentSequence = _sequence.Value;
					var puzzleEnvironment = await _generatePuzzleEnvironmentTask;

					while (true)
					{
						var pieceIndexes = Sequence.GeneratePieceIndexes(currentSequence);
						var (_, badListPlacementIndex) = TryGenerateListPlacements(puzzleEnvironment, pieceIndexes);
						if (!badListPlacementIndex.HasValue)
						{
							break;
						}
						int badIndex = Sequence.ListPlacementIndexToSequenceIndex(badListPlacementIndex.Value);
						currentSequence = Sequence.Increment(currentSequence, badIndex);
						_sequence.OnNext(currentSequence);
					}
				}
			);
		}

		private static Rotation[] GetPossibleRotations(
			PuzzleEnvironment puzzleEnvironment,
			int positionIndex,
			int pieceIndex,
			Placement?[] listPlacements
		)
		{
			var sides = puzzleEnvironment.PieceSides[pieceIndex];
			var edgeRequirements = PuzzleSolver.GetEdgeRequirements(
				puzzleEnvironment,
				listPlacements,
				positionIndex
			);
			return edgeRequirements.SelectMany(
				er => PuzzleSolver.GetRotations(sides, er)
			).ToArray();
		}

		private static Placement?[]? TryAddPiece(
			PuzzleEnvironment puzzleEnvironment,
			Placement?[] existingPlacements,
			int positionIndex,
			int pieceIndex
			)
		{
			var listPlacements = new Placement?[existingPlacements.Length];
			existingPlacements.CopyTo(listPlacements, 0);
				
			var rotations = GetPossibleRotations(
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
					var thisRotations = GetPossibleRotations(
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
					}
				}
			}
			return listPlacements;
		}

		private static (Placement?[], int?) TryGenerateListPlacements(
			PuzzleEnvironment puzzleEnvironment,
			int[] pieceIndexes
		)
		{
			Placement?[] listPlacements = new Placement?[256];
			int? firstFailedIndex = default;
			for (int positionIndex = 0; positionIndex < pieceIndexes.Length; ++positionIndex)
			{
				var pieceIndex = pieceIndexes[positionIndex];
				Placement?[]? newPlacements = TryAddPiece(
					puzzleEnvironment,
					listPlacements,
					positionIndex,
					pieceIndex
				);
				if (newPlacements == null)
				{
					return (listPlacements, positionIndex);
				}
				listPlacements = newPlacements;
			}
			return (listPlacements, firstFailedIndex);

		}

		private static Placement?[] GenerateListPlacements(
			PuzzleEnvironment puzzleEnvironment, 
			int[] pieceIndexes
		)
		{
			var (listPlacements, firstFailure) = TryGenerateListPlacements(puzzleEnvironment, pieceIndexes);
			return listPlacements;
		}

		private void GenerateRandom()
		{
			this._sequence.OnNext(Sequence.GenerateRandomSequence());
		}


		BehaviorSubject<int[]> _sequence = new BehaviorSubject<int[]>(Sequence.FirstSequence);

		Task<PuzzleEnvironment> _generatePuzzleEnvironmentTask = PuzzleEnvironment.Generate();


		private static string AsTwoDigitHex(int n)
		{
			var unpaddedHex = $"{n:X}";
			return unpaddedHex.Length == 1 ? $"0{unpaddedHex}" : unpaddedHex;
		}
		private static string SequenceToString(IEnumerable<int> sequence) =>
			string.Join(' ', sequence.Select(AsTwoDigitHex));

		public string SequenceAsString { get; set; } = string.Empty;

		public MainWindowViewModel()
		{
			var puzzleEnvironmentObservable = _generatePuzzleEnvironmentTask.ToObservable();

			var sampledSequence = _sequence.Sample(TimeSpan.FromSeconds(0.5))
				.ObserveOn(SynchronizationContext.Current!);

			
			var canvasItemObservable =
				from pieceIndexes in (
					from s in sampledSequence
					select Sequence.GeneratePieceIndexes(s)
				)
				from puzzleEnvironment in puzzleEnvironmentObservable
				let listPlacements = GenerateListPlacements(puzzleEnvironment, pieceIndexes)
				select GenerateCanvasItems(puzzleEnvironment, listPlacements);

			canvasItemObservable
				.Subscribe(
					canvasItems =>
					{
						Debug.WriteLine("Updating the canvas items");
						this.CanvasItems = canvasItems;
						this._propertyChangedEventHandler?.Invoke(this, new(nameof(CanvasItems)));
					}
				);

			_sequence.Sample(TimeSpan.FromSeconds(0.1))
				.ObserveOn(SynchronizationContext.Current!)
				.Select(SequenceToString)
				.Subscribe(
				s =>
				{
					this.SequenceAsString = s;
					this._propertyChangedEventHandler?.Invoke(this, new(nameof(SequenceAsString)));
				}
			);

		}
	}
}

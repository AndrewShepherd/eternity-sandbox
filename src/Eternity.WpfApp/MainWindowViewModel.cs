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
	using System.Windows.Markup;
	using System.Windows.Threading;
	using System.Text.Json.Serialization;
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

		private static (IEnumerable<Placement>, int?) TryGenerateListPlacements(
			PuzzleEnvironment puzzleEnvironment,
			int[] pieceIndexes
		)
		{
			List<Placement> listPlacements = new List<Placement>();
			int? firstFailedIndex = default;
			for (int i = 0; i < pieceIndexes.Length; ++i)
			{
				var pieceIndex = pieceIndexes[i];
				var position = puzzleEnvironment.PositionLookup[i];
				var sides = puzzleEnvironment.PieceSides[pieceIndex];

				var edgeRequirements = PuzzleSolver.GetEdgeRequirements(
					puzzleEnvironment,
					listPlacements,
					i
				);
				IEnumerable<Rotation> rotations = PuzzleSolver.GetRotations(sides, edgeRequirements);
				if (!rotations.Any())
				{
					firstFailedIndex = firstFailedIndex ?? i;
				}
				listPlacements.Add(new Placement(pieceIndex, rotations.FirstOrDefault()));
			}
			return (listPlacements, firstFailedIndex);

		}

		private static IEnumerable<Placement> GenerateListPlacements(
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


		BehaviorSubject<int[]> _sequence = new BehaviorSubject<int[]>(Sequence.GenerateRandomSequence());

		Task<PuzzleEnvironment> _generatePuzzleEnvironmentTask = PuzzleEnvironment.Generate();
		public MainWindowViewModel()
		{
			var puzzleEnvironmentObservable = _generatePuzzleEnvironmentTask.ToObservable();

			var canvasItemObservable =
				from pieceIndexes in (
					from s in _sequence.Sample(TimeSpan.FromSeconds(1.0))
					select Sequence.GeneratePieceIndexes(s)
				)
				from puzzleEnvironment in puzzleEnvironmentObservable
				let listPlacements = GenerateListPlacements(puzzleEnvironment, pieceIndexes)
				select GenerateCanvasItems(puzzleEnvironment, listPlacements);

			canvasItemObservable
				//.ObserveOn(SynchronizationContext.Current!)
				//.Throttle(TimeSpan.FromSeconds(1.0))
				.Subscribe(
					canvasItems =>
					{
						Debug.WriteLine("Updating the canvas items");
						this.CanvasItems = canvasItems;
						this._propertyChangedEventHandler?.Invoke(this, new(nameof(CanvasItems)));
					}
				);

			
		}
	}
}

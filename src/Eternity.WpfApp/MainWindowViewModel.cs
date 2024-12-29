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


		private static IEnumerable<Placement> GenerateListPlacements(
			PuzzleEnvironment puzzleEnvironment, 
			int[] pieceIndexes
		)
		{
			List<Placement> listPlacements = new List<Placement>();
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

				listPlacements.Add(new Placement(pieceIndex, rotations.FirstOrDefault()));
			}
			return listPlacements;
		}

		private void GenerateRandom()
		{
			this._sequence.OnNext(Sequence.GenerateRandomSequence());
		}


		BehaviorSubject<int[]> _sequence = new BehaviorSubject<int[]>(Sequence.GenerateRandomSequence());

		public MainWindowViewModel()
		{
			var puzzleEnvironmentObservable = PuzzleEnvironment.Generate().ToObservable();

			var canvasItemObservable =
				from pieceIndexes in (from s in _sequence select Sequence.GeneratePieceIndexes(s))
				from puzzleEnvironment in puzzleEnvironmentObservable
				let listPlacements = GenerateListPlacements(puzzleEnvironment, pieceIndexes)
				select GenerateCanvasItems(puzzleEnvironment, listPlacements);

			canvasItemObservable.Subscribe(
				canvasItems =>
				{
					this.CanvasItems = canvasItems;
					this._propertyChangedEventHandler?.Invoke(this, new(nameof(CanvasItems)));
				}
			);

			
		}
	}
}

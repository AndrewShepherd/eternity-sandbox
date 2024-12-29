namespace Eternity.WpfApp
{
	using Prism.Commands;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.ComponentModel;
	using System.Windows.Input;
	using static Eternity.WpfApp.CanvasItemExtensions;
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


		private async void GenerateRandom()
		{
			var puzzleEnvironment = await PuzzleEnvironment.Generate();

			var sequence = Sequence.GenerateRandomSequence();

			var pieceIndexes = Sequence.GeneratePieceIndexes(sequence);

			List<Placement> l = new List<Placement>();
			for(int i = 0; i < pieceIndexes.Length; ++i)
			{
				var pieceIndex = pieceIndexes[i];
				var position = puzzleEnvironment.PositionLookup[i];
				var sides = puzzleEnvironment.PieceSides[pieceIndex];

				var edgeRequirements = PuzzleSolver.GetEdgeRequirements(
					puzzleEnvironment,
					l,
					i
				);
				IEnumerable<Rotation> rotations = PuzzleSolver.GetRotations(sides, edgeRequirements);

				l.Add(new Placement(pieceIndex, rotations.FirstOrDefault()));
			}
			this.CanvasItems = GenerateCanvasItems(puzzleEnvironment, l);
			this._propertyChangedEventHandler?.Invoke(this, new(nameof(CanvasItems)));
		}



		public MainWindowViewModel()
		{
			GenerateRandom();
		}
	}
}

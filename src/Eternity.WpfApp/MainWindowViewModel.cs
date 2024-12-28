using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Eternity.WpfApp
{
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


		private static IEnumerable<CanvasItem> GenerateCanvasItems(PuzzleEnvironment environment, IEnumerable<Placement> placements)
		{

			var firstImage = environment.Images[0];
			var imageWidth = firstImage.Width;
			var imageHeight = firstImage.Height;

			var canvasItems = placements.Select(
				(placement, index) =>
				{
					var position = environment.PositionLookup[index];
					return new CanvasItem
					{
						ImageSource = environment.Images[placement.PieceIndex],
						Left = position.X * imageWidth,
						Top = position.Y * imageHeight,
						Rotation = (int)placement.rotation * 90,
					};
				}
			).ToList();
			return canvasItems;
		}

		private async void LoadImages()
		{
			var puzzleEnvironment = await PuzzleEnvironment.Generate();
			List<Placement> l = new List<Placement>();


			Dictionary<Position, int> reversePositionLookup = puzzleEnvironment.PositionLookup.Select(
				(position, index) => KeyValuePair.Create(position, index)
				).ToDictionary();
			for(int i = 0; i < puzzleEnvironment.PieceSides.Length; ++i)
			{
				var position = puzzleEnvironment.PositionLookup[i];
				var sides = puzzleEnvironment.PieceSides[i];

				var edgeRequirements = PuzzleSolver.GetEdgeRequirements(
					puzzleEnvironment,
					reversePositionLookup,
					l,
					i
				);
				IEnumerable<Rotation> rotations = PuzzleSolver.GetRotations(sides, edgeRequirements);

				l.Add(new Placement(i, rotations.FirstOrDefault()));
			}
			this.CanvasItems = GenerateCanvasItems(puzzleEnvironment, l);
			this._propertyChangedEventHandler?.Invoke(this, new(nameof(CanvasItems)));
		}



		public MainWindowViewModel()
		{
			LoadImages();
		}
	}
}

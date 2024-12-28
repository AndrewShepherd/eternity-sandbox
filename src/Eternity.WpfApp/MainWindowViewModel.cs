using System;
using System.Collections.Generic;
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


		private static IEnumerable<CanvasItem> GenerateCanvasItems(PuzzleEnvironment environment)
		{
			CanvasItem[] result = new CanvasItem[environment.Images.Length];
			var firstImage = environment.Images[0];
			var imageWidth = firstImage.Width;
			var imageHeight = firstImage.Height;
			for(int i = 0; i < environment.Images.Length; ++i)
			{
				var image = environment.Images[i];
				var position = environment.PositionLookup[i];
				result[i] = new CanvasItem
				{
					ImageSource = image,
					Left = position.X * imageWidth,
					Top = position.Y * imageHeight
				};
			}
			return result;
		}

		private async void LoadImages()
		{
			var puzzleEnvironment = await PuzzleEnvironment.Generate();
			this.CanvasItems = GenerateCanvasItems(puzzleEnvironment);
			this._propertyChangedEventHandler?.Invoke(this, new(nameof(CanvasItems)));
		}


		public MainWindowViewModel()
		{
			var pe = PuzzleEnvironment.Generate().Result;
			LoadImages();
		}
	}
}

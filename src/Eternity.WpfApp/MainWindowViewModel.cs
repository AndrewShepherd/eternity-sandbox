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

		int _gridWidth = 0;
		public int GridWidth
		{
			get => _gridWidth;
			set
			{
				_gridWidth = value;
			}
		}

		public long GridHeight
		{
			get;
			set;	
		}

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

		private async void LoadImages()
		{
			var task = PuzzleProvider.LoadPieces()
				.ContinueWith(
				x =>
				{
					List<BitmapImage> images = new List<BitmapImage>();
					foreach (var piece in x.Result)
					{
						using (var stream = ImageProvider.Load(piece.ImageId))
						{
							images.Add(CreateFromStream(stream!));
						}
					}
					return images;
				}
			).ContinueWith(
				x =>
				{
					var result = x.Result;
					var firstImage = result.First()!;
					var imageHeight = firstImage.Height;
					var imageWidth = firstImage.Width;

					var rv = new List<CanvasItem>();
					for (int r = 0; r < 16; ++r)
					{
						for (int c = 0; c < 16; ++c)
						{
							var image = result[r * 16 + c];
							var canvasItem = new CanvasItem
							{
								ImageSource = image,
								Left = (int)(c * imageWidth),
								Top = (int)(r * imageHeight),
							};
							rv.Add(canvasItem);
						}
					}
					return rv;
				}
			);
			this.CanvasItems = await task;
			this._propertyChangedEventHandler?.Invoke(
				this,
				new PropertyChangedEventArgs(nameof(this.CanvasItems))
			);
		}


		public MainWindowViewModel()
		{
			LoadImages();
		}
	}
}

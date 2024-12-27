using System;
using System.Collections.Generic;
using System.ComponentModel;
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

		public ImageSource? ImageSource 
		{ 
			get; 
			set; 
		}

		public MainWindowViewModel()
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			var resourceNames = ImageProvider.GetResourceNames();
			var imageResourceName = resourceNames.OrderBy(n => n).First();
			var resourceStream = ImageProvider.Load(imageResourceName);
			if (resourceStream != null)
			{
				using (resourceStream)
				{
					var bitmap = new BitmapImage();
					bitmap.BeginInit();
					bitmap.StreamSource = resourceStream;
					bitmap.CacheOption = BitmapCacheOption.OnLoad;
					bitmap.EndInit();
					bitmap.Freeze();

					this.ImageSource = bitmap;
				}
			}
			



		}
	}
}

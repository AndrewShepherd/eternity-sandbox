
namespace Eternity.WpfApp
{
	using System.Windows.Media;
	using System.Windows.Media.Imaging;

	public class CanvasItem
	{
		public BitmapImage? ImageSource { get; set; }

		public double Top { get; set; }
		public double Left { get; set; }
	}
}

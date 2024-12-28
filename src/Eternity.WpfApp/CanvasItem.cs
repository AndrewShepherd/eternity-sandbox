
namespace Eternity.WpfApp
{
	using System.Windows.Media;
	using System.Windows.Media.Imaging;

	public class CanvasItem
	{
		public BitmapImage? ImageSource { get; set; }

		public int Top { get; set; }
		public int Left { get; set; }
	}
}

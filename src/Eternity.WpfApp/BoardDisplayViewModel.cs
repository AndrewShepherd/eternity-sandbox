using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternity.WpfApp
{
	public sealed class BoardDisplayViewModel : BindableBase
	{
		private IEnumerable<CanvasItem> _canvasItems = Enumerable.Empty<CanvasItem>();

		public IEnumerable<CanvasItem> CanvasItems
		{
			get => _canvasItems;
			set => SetProperty(ref _canvasItems, value);
		}
	}
}

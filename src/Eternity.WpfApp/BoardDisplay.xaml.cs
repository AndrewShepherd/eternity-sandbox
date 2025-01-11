using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Eternity.WpfApp
{
	/// <summary>
	/// Interaction logic for BoardDisplay.xaml
	/// </summary>
	public partial class BoardDisplay : UserControl
	{
		public BoardDisplay()
		{
			InitializeComponent();
		}

		public IEnumerable<CanvasItem>? CanvasItems
		{ 
			get => GetValue(CanvasItemsProperty) as IEnumerable<CanvasItem>;
			set => SetValue(CanvasItemsProperty, value);
		}

		public static DependencyProperty CanvasItemsProperty = DependencyProperty.Register(
			nameof(CanvasItems),
			typeof(IEnumerable<CanvasItem>),
			typeof(BoardDisplay),
			new PropertyMetadata
			{
				DefaultValue = Enumerable.Empty<CanvasItem>(),
				PropertyChangedCallback = (o, e) =>
				{
					BoardDisplay? boardDisplay = o as BoardDisplay;
					var viewModel = boardDisplay?.ViewModel;
					if (viewModel != null)
					{
						viewModel.CanvasItems = (e.NewValue as IEnumerable<CanvasItem>) ?? Enumerable.Empty<CanvasItem>();
					}
				}
			}
		);

		private BoardDisplayViewModel? ViewModel
		{
			get => this.Resources["BoardDisplayViewModel"] as BoardDisplayViewModel;
		}
	}
}

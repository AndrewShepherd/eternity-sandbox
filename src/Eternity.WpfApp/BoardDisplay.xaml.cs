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
			this.SizeChanged += BoardDisplay_SizeChanged;
		}

		private void BoardDisplay_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var viewModel = this.ViewModel;
			if (viewModel != null)
			{
				viewModel.CanvasSize = e.NewSize;
			}
		}

		private static void UpdateViewModel(DependencyObject o, Action<BoardDisplayViewModel> action)
		{
			BoardDisplay? boardDisplay = o as BoardDisplay;
			var viewModel = boardDisplay?.ViewModel;
			if (viewModel != null)
			{
				action(viewModel);
			}
		}

		public Placements? Placements
		{
			get => GetValue(PlacementsProperty) as Placements;
			set => SetValue(PlacementsProperty, value);
		}

		public static DependencyProperty PlacementsProperty = DependencyProperty.Register(
			nameof(Placements),
			typeof(Placements),
			typeof(BoardDisplay),
			new PropertyMetadata
			{
				PropertyChangedCallback = (o, e) => UpdateViewModel(
					o,
					vm => vm.Placements = e.NewValue as Placements
				)
			}
		);

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
					UpdateViewModel(
						o,
						vm => vm.CanvasItems = (e.NewValue as IEnumerable<CanvasItem>) ?? Enumerable.Empty<CanvasItem>()
					);
				}
			}
		);

		public int SelectedSequenceIndex
		{
			get => (int)GetValue(SelectedSequenceIndexProperty);
			set => SetValue(SelectedSequenceIndexProperty, value);
		}

		public static DependencyProperty SelectedSequenceIndexProperty = DependencyProperty.Register(
			nameof(SelectedSequenceIndex),
			typeof(int),
			typeof(BoardDisplay),
			new PropertyMetadata
			{
				DefaultValue = -1,
				PropertyChangedCallback = (o, e) =>
				{
					UpdateViewModel(
						o,
						vm => vm.SelectedSequenceIndex = (int)(e.NewValue)
					);
				}
			}
		);

		private BoardDisplayViewModel? ViewModel
		{
			get => this.Resources["BoardDisplayViewModel"] as BoardDisplayViewModel;
		}

		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
		}
	}
}

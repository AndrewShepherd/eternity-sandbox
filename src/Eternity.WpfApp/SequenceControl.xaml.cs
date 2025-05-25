using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Eternity.WpfApp
{
	using System.ComponentModel;
	using System.Security.Policy;
	using System.Windows.Controls;

	/// <summary>
	/// Interaction logic for SequenceControl.xaml
	/// </summary>
	public partial class SequenceControl : UserControl
	{
		public SequenceControl()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			var notifyPropertyChanaged = this.SequenceControlViewModel as INotifyPropertyChanged;
			if (notifyPropertyChanaged != null)
			{
				notifyPropertyChanaged.PropertyChanged += (sender, args) =>
				{
					if (args.PropertyName == nameof(SequenceControlViewModel.SelectedSequenceIndex))
					{
						var newValue = this.SequenceControlViewModel!.SelectedSequenceIndex;
						SetValue(
							SelectedSequenceIndexProperty,
							newValue
						);
					}
				};
			}
		}

		public IReadOnlyList<StackEntry> StackEntries
		{
			get => (IReadOnlyList<StackEntry>)GetValue(StackEntriesProperty);
			set => SetValue(StackEntriesProperty, value);
		}

		private SequenceControlViewModel? SequenceControlViewModel =>
			Resources["SequenceControlViewModel"] as SequenceControlViewModel;

		public int SelectedSequenceIndex
		{
			get => (int)GetValue(SelectedSequenceIndexProperty);
			set => SetValue(SelectedSequenceIndexProperty, value);
		}

		public static readonly DependencyProperty StackEntriesProperty = DependencyProperty.Register(
			nameof(SequenceControl.StackEntries),
			typeof(IReadOnlyList<StackEntry>),
			typeof(SequenceControl),
			new PropertyMetadata
			{
				DefaultValue = new StackEntry[0],
				PropertyChangedCallback = StackEntriesChangedCallBack
			}
		);

		public static readonly DependencyProperty SelectedSequenceIndexProperty = DependencyProperty.Register(
			nameof(SelectedSequenceIndex),
			typeof(int),
			typeof(SequenceControl),
			new PropertyMetadata
			{
				DefaultValue = -1,
				PropertyChangedCallback = SelectedSequenceIndexChangedCallback
			}
		);

		private static void StackEntriesChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			SequenceControl ctrl = (SequenceControl)d;
			var viewModel = ctrl.SequenceControlViewModel;
			if (viewModel != null)
			{
				viewModel.StackEntries = (IReadOnlyList<StackEntry>)e.NewValue;
			}
		}

		private static void SelectedSequenceIndexChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			SequenceControl ctrl = (SequenceControl)d;
			var viewModel = ctrl.SequenceControlViewModel;
			if (viewModel != null)
			{
				viewModel.SelectedSequenceIndex = (int)e.NewValue;
			}
		}
	}
}

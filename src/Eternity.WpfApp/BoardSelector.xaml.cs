namespace Eternity.WpfApp
{
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;
	/// <summary>
	/// Interaction logic for BoardSelector.xaml
	/// </summary>
	public partial class BoardSelector : UserControl, INotifyPropertyChanged
	{
		public BoardSelector()
		{
			InitializeComponent();
		}

		PropertyChangedEventHandler? _propertyChangedEventHandler;
		event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
		{
			add
			{
				_propertyChangedEventHandler += value;
			}

			remove
			{
				var p = _propertyChangedEventHandler;
				if (p != null)
				{
					p -= value;
				}
			}
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			var vm = this.ViewModel;
			vm.PropertyChanged += (s, a) =>
			{
				if (a.PropertyName == nameof(BoardSelectorViewModel.SelectedPlacements))
				{
					this._propertyChangedEventHandler?.Invoke(this, new(nameof(SelectedPlacements)));
				}
			};
		}

		private BoardSelectorViewModel? ViewModel => (this.Resources["BoardSelectorViewModel"] as BoardSelectorViewModel);

		public static DependencyProperty SolutionsDependencyProperty = DependencyProperty.Register(
			nameof(Solutions),
			typeof(IReadOnlyList<Placements>),
			typeof(BoardSelector)
		);

		public static DependencyProperty WorkingPlacementsDependencyProperty = DependencyProperty.Register(
			nameof(WorkingPlacements),
			typeof(Placements),
			typeof(BoardSelector),
			new PropertyMetadata()
			{
				PropertyChangedCallback = (o, v) =>
				{
					(o as BoardSelector)!.WorkingPlacements = (Eternity.Placements)v.NewValue;
				}
			}

		);
		public IReadOnlyList<Placements>? Solutions
		{
			get => this.ViewModel?.Solutions;
			set
			{
				var vm = this.ViewModel;
				if (vm != null)
				{
					vm.Solutions = value;
				}
			}
		}

		public Placements? WorkingPlacements
		{
			get => this.ViewModel?.WorkingPlacements;
			set
			{
				var vm = this.ViewModel;
				if (vm != null)
				{
					vm.WorkingPlacements = value;
				}
			}
		}

		public Placements? SelectedPlacements
		{
			get => this.ViewModel?.SelectedPlacements;
		}
	}
}

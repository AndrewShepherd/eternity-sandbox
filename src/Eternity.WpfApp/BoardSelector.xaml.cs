using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	/// Interaction logic for BoardSelector.xaml
	/// </summary>
	public partial class BoardSelector : UserControl, INotifyPropertyChanged
	{
		public BoardSelector()
		{
			InitializeComponent();
		}

		PropertyChangedEventHandler _propertyChangedEventHandler;
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
				PropertyChangedCallback = (o, v) => (o as BoardSelector)!.WorkingPlacements = (Eternity.Placements)v.NewValue
			}

		);
		public IReadOnlyList<Placements> Solutions
		{
			get;
			set;
		}

		private Placements? _workingPlacements = null;
		public Placements? WorkingPlacements
		{
			get => _workingPlacements;
			set
			{
				_workingPlacements = value;
				this.SelectedPlacements = value;
			}
		}

		private Placements? _selectedPlacements;
		public Placements? SelectedPlacements
		{
			get => _selectedPlacements;
			set
			{
				if (_selectedPlacements != value)
				{
					_selectedPlacements = value;
					_propertyChangedEventHandler?.Invoke(
						this,
						new PropertyChangedEventArgs(nameof(SelectedPlacements))
					);
				}
			}
		}
	}
}

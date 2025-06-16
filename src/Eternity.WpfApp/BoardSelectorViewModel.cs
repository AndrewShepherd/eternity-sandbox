

namespace Eternity.WpfApp
{
	using Eternity;
	using ReactiveUI;
	using System.Collections.Generic;

	class BoardSelectorViewModel : ReactiveObject
	{
		readonly ObservableAsPropertyHelper<Placements?> _selectedPlacements;
		

		public BoardSelectorViewModel()
		{
			_selectedPlacements = this.WhenAnyValue(
				vm => vm.WorkingPlacements
			).ToProperty(this, vm => vm.SelectedPlacements, out _selectedPlacements);
		}
		public IReadOnlyList<Placements>? Solutions 
		{ 
			get; 
			internal set;
		}

		private Placements? _workingPlacements = null;
		public Placements? WorkingPlacements
		{
			get => _workingPlacements;
			internal set
			{
				this.RaiseAndSetIfChanged(ref _workingPlacements, value, nameof(WorkingPlacements));
			}
		}
		public Placements? SelectedPlacements => _selectedPlacements.Value;
	}
}

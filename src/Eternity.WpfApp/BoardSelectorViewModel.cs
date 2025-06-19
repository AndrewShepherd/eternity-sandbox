namespace Eternity.WpfApp
{
	using Eternity;
	using ReactiveUI;
	using System.Collections.Generic;
	using System.Reactive.Linq;

	class BoardSelectorViewModel : ReactiveObject
	{
		public enum Selection
		{
			WorkingSolution,
			CompleteSolution
		}

		private Selection _currentSelection = Selection.WorkingSolution;
		public Selection CurrentSelection
		{
			get => _currentSelection;
			set
			{
				this.RaiseAndSetIfChanged(ref _currentSelection, value);
			}
		}

		readonly ObservableAsPropertyHelper<Placements?> _selectedPlacements;
		readonly ObservableAsPropertyHelper<string> _solutionsText;
		readonly ObservableAsPropertyHelper<bool> _showCurrentWorking;
		readonly ObservableAsPropertyHelper<bool> _showSolution;
		readonly ObservableAsPropertyHelper<bool> _showSolutionEnabled;

		public BoardSelectorViewModel()
		{
			_solutionsText = this.WhenAnyValue(
				vm => vm.Solutions
			).Select(
				s =>
					s switch
					{
						IReadOnlyList<Placements> l => $"{l.Count} Solutions",
						_ => string.Empty
					}
			).ToProperty(
				this,
				vm => vm.SolutionsText,
				out _solutionsText
			);

			this.WhenAnyValue(
				vm => vm.CurrentSelection
			).Select(
				v => v == Selection.WorkingSolution
			).ToProperty(
				this,
				vm => vm.ShowCurrentWorking,
				out _showCurrentWorking
			);

			this.WhenAnyValue(
				vm => vm.CurrentSelection
			).Select(
				v => v == Selection.CompleteSolution
			).ToProperty(
				this,
				vm => vm.ShowSolution,
				out _showSolution
			);

			this.WhenAnyValue(
				vm => vm.Solutions
			).Select(
				s => s is IReadOnlyList<Placements> l && l.Count > 0
			).ToProperty(
				this,
				vm => vm.ShowSolutionEnabled,
				out _showSolutionEnabled
			);

			IObservable<Selection> currentSelectionObservable = this.WhenAnyValue(vm => vm.CurrentSelection);
			IObservable<Placements?> workingPlacementsObservable = this.WhenAnyValue(vm => vm.WorkingPlacements);
			IObservable<Placements?> completeSolutionObservable = this.WhenAnyValue(vm => vm.Solutions)
				.Select(
					s =>
						s switch
						{
							IReadOnlyList<Placements> l when l.Count > 0 => l[0],
							_ => null
						}
				);

			var selectedPlacementsObservable = currentSelectionObservable
				.Select(selection =>
					selection switch
					{
						Selection.WorkingSolution => workingPlacementsObservable,
						Selection.CompleteSolution => completeSolutionObservable,
						_ => Observable.Return<Placements?>(null)
					}
				).Switch();

			selectedPlacementsObservable.ToProperty(
				this,
				vm => vm.SelectedPlacements,
				out _selectedPlacements
			);
		}

		IReadOnlyList<Placements>? _solutions;
		public IReadOnlyList<Placements>? Solutions 
		{
			get => _solutions;
			internal set {
				this.RaiseAndSetIfChanged(ref _solutions, value, nameof(Solutions));
			}
		}

		public string SolutionsText => _solutionsText.Value;

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

		public bool ShowCurrentWorking
		{
			get => _showCurrentWorking.Value;
			set
			{
				this.CurrentSelection = value ? Selection.WorkingSolution : Selection.CompleteSolution;
			}
		}

		public bool ShowSolution
		{
			get => _showSolution.Value;
			set
			{
				this.CurrentSelection = value ? Selection.CompleteSolution : Selection.WorkingSolution;
			}
		}

		public bool ShowSolutionEnabled => _showSolutionEnabled.Value;
	}
}

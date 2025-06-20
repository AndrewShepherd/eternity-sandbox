namespace Eternity.WpfApp
{
	using Eternity;
	using ReactiveUI;
	using System.Collections.Generic;
	using System.Reactive.Linq;
	using System.Windows.Data;

	public class BoardSelectorViewModel : ReactiveObject
	{
		public enum Selection
		{
			WorkingSolution,
			CompleteSolution
		}

		// Converters for radio button binding
		public static readonly IValueConverter WorkingSolutionConverter = SelectionToBooleanConverter.Create(Selection.WorkingSolution);
		public static readonly IValueConverter CompleteSolutionConverter = SelectionToBooleanConverter.Create(Selection.CompleteSolution);

		Selection _currentSelection = Selection.WorkingSolution;
		IReadOnlyList<Placements>? _solutions;
		Placements? _workingPlacements = null;
		readonly ObservableAsPropertyHelper<Placements?> _selectedPlacements;
		readonly ObservableAsPropertyHelper<string> _solutionsText;
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
				vm => vm.SolutionsText
			);

			_showSolutionEnabled = this.WhenAnyValue(
				vm => vm.Solutions
			).Select(
				s => s is IReadOnlyList<Placements> l && l.Count > 0
			).ToProperty(
				this,
				vm => vm.ShowSolutionEnabled
			);

			var completeSolutionObservable = this.WhenAnyValue(vm => vm.Solutions)
				.Select(
					s =>
						s switch
						{
							IReadOnlyList<Placements> l when l.Count > 0 => l[0],
							_ => null
						}
				);

			_selectedPlacements = this.WhenAnyValue(vm => vm.CurrentSelection)
				.Select(selection =>
					selection switch
					{
						Selection.WorkingSolution => this.WhenAnyValue(vm => vm.WorkingPlacements),
						Selection.CompleteSolution => completeSolutionObservable,
						_ => Observable.Return<Placements?>(null)
					}
				).Switch()
				.ToProperty(
					this,
					vm => vm.SelectedPlacements
				);
		}

		public IReadOnlyList<Placements>? Solutions 
		{
			get => _solutions;
			internal set {
				this.RaiseAndSetIfChanged(ref _solutions, value, nameof(Solutions));
			}
		}

		public Selection CurrentSelection
		{
			get => _currentSelection;
			set => this.RaiseAndSetIfChanged(ref _currentSelection, value);
		}

		public string SolutionsText => _solutionsText.Value;

		public Placements? WorkingPlacements
		{
			get => _workingPlacements;
			internal set => 
				this.RaiseAndSetIfChanged(
					ref _workingPlacements,
					value,
					nameof(WorkingPlacements)
				);
		}
		public Placements? SelectedPlacements => _selectedPlacements.Value;

		public bool ShowSolutionEnabled => _showSolutionEnabled.Value;
	}
}

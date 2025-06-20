namespace Eternity.WpfApp
{
	using Eternity;
	using ReactiveUI;
	using System.Collections.Generic;
	using System.Reactive.Linq;
	using System.Windows.Data;
	using System.Windows.Input;

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
		int? _solutionIndex = 1;
		readonly ObservableAsPropertyHelper<Placements?> _selectedPlacements;
		readonly ObservableAsPropertyHelper<string> _solutionsText;
		readonly ObservableAsPropertyHelper<bool> _showSolutionEnabled;

		public ICommand PreviousSolutionCommand { get; }
		public ICommand NextSolutionCommand { get; }

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

			var canExecutePrevious = this.WhenAnyValue(
				vm => vm.SolutionIndex,
				vm => vm.Solutions,
				(index, solutions) => index.HasValue && index > 1 && solutions is not null && solutions.Count > 0
			);

			PreviousSolutionCommand = ReactiveCommand.Create(
				() =>
				{
					if (SolutionIndex.HasValue)
					{
						SolutionIndex--;
					}
				},
				canExecutePrevious
			);

			var canExecuteNext = this.WhenAnyValue(
				vm => vm.SolutionIndex,
				vm => vm.Solutions,
				(index, solutions) => index.HasValue && solutions is not null && index < solutions.Count
			);

			NextSolutionCommand = ReactiveCommand.Create(
				() =>
				{
					if (SolutionIndex.HasValue)
					{
						SolutionIndex++;
					}
				},
				canExecuteNext
			);

			var completeSolutionObservable = Observable.CombineLatest(
				this.WhenAnyValue(vm => vm.Solutions),
				this.WhenAnyValue(vm => vm.SolutionIndex),
				(solutions, index) =>
				{
					if ((solutions == null) || (solutions.Count == 0))
					{
						return default;
					}

					return index switch
					{
						null => solutions.Count > 0 ? solutions[0] : null,
						int n when n < 1 => solutions[0],
						int n when n > solutions.Count => solutions[solutions.Count - 1],
						int n => solutions[n - 1]
					};
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

		public int? SolutionIndex
		{
			get => _solutionIndex;
			set => this.RaiseAndSetIfChanged(ref _solutionIndex, value, nameof(SolutionIndex));
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

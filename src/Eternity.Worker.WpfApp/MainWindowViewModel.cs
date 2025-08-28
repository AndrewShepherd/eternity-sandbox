#nullable enable

namespace Eternity.Worker.WpfApp;

using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using ReactiveUI;

public sealed class MainWindowViewModel : ReactiveObject
{
	private readonly BehaviorSubject<WorkerState> _workerState = new(
		new WorkerStateIdle()
	);

	private ReactiveCommand<Unit, Unit> _toggleConnectionCommand;

	readonly ObservableAsPropertyHelper<string> _toggleConnectionDescription;

	private static async Task<WorkerState> Toggle(WorkerState state) =>
		state switch
		{
			WorkerStateIdle i => i.Connect(),
			WorkerStateConnected c => await c.Disconnect(),
			_ => throw new Exception("Unexpected worker state")
		};
	public MainWindowViewModel()
	{
		_toggleConnectionDescription = (
			from s in _workerState
			select s switch
			{
				WorkerStateIdle => "Connect",
				WorkerStateConnected => "Disconnect",
				_ => "Unknown state"
			}
		).ToProperty(this, vm => vm.ToggleConnectionText);

		_toggleConnectionCommand = ReactiveCommand.Create(
			async void () => _workerState.OnNext(await Toggle(_workerState.Value))
		);
	}

	public string ToggleConnectionText => _toggleConnectionDescription.Value;
	public ICommand ToggleConnectionCommand => _toggleConnectionCommand;
}

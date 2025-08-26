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
			() =>
				_workerState.OnNext(
					_workerState.Value switch
					{
						WorkerStateIdle i => i.Connect(),
						WorkerStateConnected c => c.Disconnect(),
						_ => throw new Exception("Unexpected worker state")
					}
				)
		);
	}

	public string ToggleConnectionText => _toggleConnectionDescription.Value;
	public ICommand ToggleConnectionCommand => _toggleConnectionCommand;
}

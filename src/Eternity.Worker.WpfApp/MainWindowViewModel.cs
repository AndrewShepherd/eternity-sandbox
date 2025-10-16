#nullable enable

namespace Eternity.Worker.WpfApp;

using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;
using System.Windows.Input;

using StateChangeDelegate = Func<IConnectionState, Task<IConnectionState>>;

public sealed class MainWindowViewModel : ReactiveObject
{
	private readonly ConnectionStateContext _workerStateContext = new();
	private readonly BehaviorSubject<IConnectionState> _connectionState;


	private ReactiveCommand<Unit, Unit> _toggleConnectionCommand;

	readonly ObservableAsPropertyHelper<string> _toggleConnectionDescription;
	readonly ObservableAsPropertyHelper<Placements> _placements;

	private Channel<StateChangeDelegate> _stateChangingEvents = Channel.CreateUnbounded<StateChangeDelegate>(
		new UnboundedChannelOptions
		{
			SingleReader = true,
			SingleWriter = false,
			AllowSynchronousContinuations = true,
		}
	);

	private readonly ServiceCollection _serviceCollection = new();


	private static void RegisterServices(IServiceCollection serviceCollection)
	{

	}

	public MainWindowViewModel()
	{
		_connectionState = new(
			new ConnectionStateIdle(_workerStateContext)
		);
		RegisterServices(_serviceCollection);
		_toggleConnectionDescription = (
			from s in _connectionState
			select s switch
			{
				ConnectionStateIdle => "Connect",
				ConnectionStateConnected => "Disconnect",
				ConnectionStatePendingRetry => "Connection Attempt Pending...",
				_ => "Unknown state"
			}
		).ToProperty(this, vm => vm.ToggleConnectionText);



		_toggleConnectionCommand = ReactiveCommand.Create(
			() =>
			{
				_stateChangingEvents.Writer.TryWrite(
					c => c.Toggle()
				);
			}
		);

		_placements = _workerStateContext.Placements.ToProperty(this, vm => vm.Placements);
		_workerStateContext.Events.Subscribe(
			e => _stateChangingEvents.Writer.TryWrite(
				currentState => OnStateContextEvent(currentState, e)
			)
		);

		HandleStateChangingEvents(_stateChangingEvents.Reader);

		_stateChangingEvents.Writer.TryWrite(
			d => (d is ConnectionStateIdle wsi) ? wsi.Connect() : Task.FromResult(d)
		);
	}

	private async void HandleStateChangingEvents(ChannelReader<StateChangeDelegate> cr)
	{
		while(await cr.WaitToReadAsync())
		{
			while(cr.TryRead(out var d))
			{
				var current = _connectionState.Value;
				var next = await d(current);
				_connectionState.OnNext(next);
			}
		}
	}

	private static Task<IConnectionState> OnStateContextEvent(
		IConnectionState current,
		ConnectionStateEvent e
	) =>
		e switch
		{
			TimerFiredEvent tfe => current.OnTimerFired(tfe.timerId),
			ServiceUnavailableEvent => current.OnServiceUnavailable(),
			_ => Task.FromResult(current)
		};

	public Placements Placements => _placements.Value;

	public string ToggleConnectionText => _toggleConnectionDescription.Value;
	public ICommand ToggleConnectionCommand => _toggleConnectionCommand;
}

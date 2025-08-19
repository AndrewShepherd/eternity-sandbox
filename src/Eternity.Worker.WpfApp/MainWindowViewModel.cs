#nullable enable

namespace Eternity.Worker.WpfApp;

using System.Reactive;
using System.Windows.Input;

using Grpc.Core;
using ReactiveUI;

using static Eternity.Proto.EternityService;

public sealed class MainWindowViewModel
{
	private ReactiveCommand<Unit, Unit> _toggleConnectionCommand;


	public ICommand ToggleConnectionCommand => _toggleConnectionCommand;

	public MainWindowViewModel()
	{
		_toggleConnectionCommand = ReactiveCommand.Create(
			PerformToggleConnectionAction
		);
	}

	private void PerformToggleConnectionAction()
	{
		var channel = new Channel(
			"localhost",
			3876,
			ChannelCredentials.Insecure
		);
		var client = new EternityServiceClient(channel);
		var call = client.ConnectWorker();
	}
	public string TestString => "Eternity.Worker.WpfApp";

	public string ToggleConnectionText => "Toggle Connection";


}

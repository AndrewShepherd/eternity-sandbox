
namespace Eternity.Server.WpfApp;

using Eternity.Proto;
using Grpc.Core;
using ReactiveUI;
using System.Collections.Generic;

public sealed class MainWindowViewModel : ReactiveObject
{
	public string TestString => "Eternity.Server.WpfApp";


	private ObservableAsPropertyHelper<IReadOnlyList<ConnectionEntry>> _connectionEntries;

	public MainWindowViewModel()
	{
		_server = new()
		{
			Services =
			{
				EternityService.BindService(_eternityService),
			},
			Ports = { new ServerPort("localhost", 3876, ServerCredentials.Insecure) }
		};
		_connectionEntries = _eternityService.Connections.ToProperty(
			this,
			vm => vm.Connections
		);
		OpenListener();
	}

	readonly EternityServiceImpl _eternityService = new();

	readonly Server _server;

	public IReadOnlyList<ConnectionEntry> Connections => _connectionEntries.Value;

	void OpenListener()
	{
		_server.Start();
	}
}

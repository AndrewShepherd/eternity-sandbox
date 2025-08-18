
namespace Eternity.Server.WpfApp;

using Eternity.Proto;
using Grpc.Core;


public sealed class MainWindowViewModel
{
	public string TestString => "Eternity.Server.WpfApp";

	public MainWindowViewModel()
	{
		OpenListener();
	}

	Server? _server;

	void OpenListener()
	{
		_server = new()
		{
			Services =
			{
				EternityService.BindService(new EternityServiceImpl()),
			},
			Ports = { new ServerPort("localhost", 3876, ServerCredentials.Insecure) }
		};
		_server.Start();
	}
}

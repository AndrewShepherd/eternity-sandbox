namespace Eternity.Worker.WpfApp;

using Grpc.Core;
using static Eternity.Proto.EternityService;

class WorkerStateIdle : WorkerState
{
	public WorkerStateConnected Connect()
	{
		var channel = new Channel(
			"localhost",
			3876,
			ChannelCredentials.Insecure
		);
		var client = new EternityServiceClient(channel);
		var call = client.ConnectWorker();
		return new(call);
	}
}

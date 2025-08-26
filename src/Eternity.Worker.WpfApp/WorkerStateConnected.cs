using Eternity.Proto;
using Grpc.Core;


namespace Eternity.Worker.WpfApp;

class WorkerStateConnected(
	AsyncDuplexStreamingCall<MessageToServer, MessageToWorker> call
) : WorkerState
{
	public WorkerStateIdle Disconnect()
	{
		var unawaitedTask = call.RequestStream.CompleteAsync();
		return new WorkerStateIdle();
	}
}

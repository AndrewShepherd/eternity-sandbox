using Eternity.Proto;
using Grpc.Core;

namespace Eternity.Worker.WpfApp;

class WorkerStateConnected: WorkerState
{
	private AsyncDuplexStreamingCall<MessageToServer, MessageToWorker> _call;
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	private readonly Task _listeningTask;
	
	public WorkerStateConnected(
		AsyncDuplexStreamingCall<MessageToServer, MessageToWorker> call
	)
	{
		_call = call;
		_listeningTask = Listen();
	}

	private async Task Listen()
	{
		try
		{
			while (!_cancellationTokenSource.IsCancellationRequested)
			{
				await _call.ResponseStream.MoveNext(_cancellationTokenSource.Token);
				if (_cancellationTokenSource.IsCancellationRequested)
				{
					break;
				}
				var message = _call.ResponseStream.Current;
				System.Diagnostics.Debug.WriteLine("Received message");
			}
		}
		catch(RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
		{
		}
		catch(TaskCanceledException)
		{
		}
	}
	public async Task<WorkerStateIdle> Disconnect()
	{
		_cancellationTokenSource.Cancel();
		await _listeningTask;
		var unawaitedTask = _call.RequestStream.CompleteAsync();
		return new WorkerStateIdle();
	}
}

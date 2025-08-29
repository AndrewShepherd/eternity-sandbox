

namespace Eternity.Worker.WpfApp;

using Eternity;
using Eternity.Proto;
using Grpc.Core;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Placements = Eternity.Placements;

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

	BehaviorSubject<Placements> _placementsSubject = new(Eternity.Placements.None);

	public IObservable<Placements> Placements => _placementsSubject.AsObservable();

	private async Task ProcessIncomingMessage(MessageToWorker message)
	{
		if (message.MessageContentsCase == MessageToWorker.MessageContentsOneofCase.RunningState)
		{
			var solutionState = SolutionStateProto.Convert(message.RunningState);
			await this.SetSolutionState(solutionState);
		}
	}

	private Task SetSolutionState(SolutionState solutionState)
	{
		// Progress it forward one
		// Just so that we have something to show for now
		solutionState._treeNode = solutionState._treeNode.Progress(
			StackEntryExtensions.ProgressForwards,
			solutionState._pieceSides
		);
		var stackEntries = solutionState._treeNode switch
		{
			Eternity.PartiallyExploredTreeNode tn => 
				StackEntryExtensions.GetStackEntries(tn)
					.Select(e => e.StackEntry).ToList(),
			_ => []
		};

		this._placementsSubject.OnNext(
			stackEntries.LastOrDefault()?.Placements ?? Eternity.Placements.None
		);
		return Task.CompletedTask;
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
				await ProcessIncomingMessage(_call.ResponseStream.Current);
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

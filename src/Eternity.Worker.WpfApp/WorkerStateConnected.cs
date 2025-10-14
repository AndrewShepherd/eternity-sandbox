

namespace Eternity.Worker.WpfApp;

using Eternity;
using Eternity.Proto;
using Grpc.Core;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Placements = Eternity.Placements;

sealed class WorkerStateConnected: WorkerState
{
	private AsyncDuplexStreamingCall<MessageToServer, MessageToWorker> _call;
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	private readonly WorkerStateContext _context;

	public WorkerStateConnected(
		WorkerStateContext context,
		AsyncDuplexStreamingCall<MessageToServer, MessageToWorker> call
	)
	{
		_context = context;
		_call = call;
	}

	BehaviorSubject<Placements> _placementsSubject = new(Eternity.Placements.None);

	public IObservable<Placements> Placements => _placementsSubject.AsObservable();

	private async Task ProcessIncomingMessage(MessageToWorker message)
	{
		if (message.MessageContentsCase == MessageToWorker.MessageContentsOneofCase.WorkInstruction)
		{
			var workInstruction = message.WorkInstruction;
			var solutionState = SolutionStateProto.Convert(workInstruction.RunningState);
			// TODO: Set the path
			await this.SetSolutionState(solutionState, workInstruction.InitialPath);
		}
	}

	private Task SetSolutionState(SolutionState solutionState, IEnumerable<int> initialPath)
	{
		// Progress it forward one
		// Just so that we have something to show for now
		solutionState._treeNode = solutionState._treeNode.Progress(
			solutionState._pieceSides,
			initialPath
		);
		var stackEntries = solutionState._treeNode switch
		{
			Eternity.PartiallyExploredTreeNode tn => 
				StackEntryExtensions.GetStackEntries(tn, initialPath.Skip(1))
					.Select(e => e.StackEntry).ToList(),
			_ => []
		};

		this._placementsSubject.OnNext(
			stackEntries.LastOrDefault()?.Placements ?? Eternity.Placements.None
		);
		return Task.CompletedTask;
	}

	public async void Listen()
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
		catch(RpcException ex) when (ex is { StatusCode: StatusCode.Unavailable})
		{
			_context.FireEvent(new ServiceUnavailableEvent());
		}
		catch(RpcException ex) when (ex.StatusCode is StatusCode.Unknown)
		{
			_context.FireEvent(new ServiceUnavailableEvent());
		}
		catch(TaskCanceledException)
		{
		}
	}
	private Task<WorkerStateIdle> Disconnect()
	{
		_cancellationTokenSource.Cancel();
		var unawaitedTask = _call.RequestStream.CompleteAsync();
		return Task.FromResult(new WorkerStateIdle(_context));
	}

	Task<WorkerState> WorkerState.Toggle() => Disconnect().ContinueWith<WorkerState>(t => t.Result);

	Task<WorkerState> WorkerState.OnTimerFired(int timerId) => Task.FromResult<WorkerState>(this);

	Task<WorkerState> WorkerState.OnServiceUnavailable() => Task.FromResult(_context.RetryAfterDelay());
}

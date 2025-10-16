

namespace Eternity.Worker.WpfApp;

using Eternity;
using Eternity.Proto;
using Grpc.Core;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Placements = Eternity.Placements;

sealed class ConnectionStateConnected: IConnectionState
{
	private AsyncDuplexStreamingCall<MessageToServer, MessageToWorker> _call;
	private readonly CancellationTokenSource _cancellationTokenSource = new();
	private readonly ConnectionStateContext _context;

	public ConnectionStateConnected(
		ConnectionStateContext context,
		AsyncDuplexStreamingCall<MessageToServer, MessageToWorker> call
	)
	{
		_context = context;
		_call = call;
	}

	BehaviorSubject<Placements> _placementsSubject = new(Eternity.Placements.None);

	public IObservable<Placements> Placements => _placementsSubject.AsObservable();

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

	private async void Listen(IObserver<MessageToWorker> observer)
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
				observer.OnNext(_call.ResponseStream.Current);
			}
		}
		catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
		{
		}
		catch (RpcException ex) when (ex is { StatusCode: StatusCode.Unavailable })
		{
			_context.FireEvent(new ServiceUnavailableEvent());
		}
		catch (RpcException ex) when (ex.StatusCode is StatusCode.Unknown)
		{
			_context.FireEvent(new ServiceUnavailableEvent());
		}
		catch (TaskCanceledException)
		{
		}
		finally
		{
			observer.OnCompleted();
		}
	}
	public System.IObservable<MessageToWorker> Listen()
	{
		Subject<MessageToWorker> subject = new();
		Listen(subject);
		return subject;

	}
	private Task<ConnectionStateIdle> Disconnect()
	{
		_cancellationTokenSource.Cancel();
		var unawaitedTask = _call.RequestStream.CompleteAsync();
		return Task.FromResult(new ConnectionStateIdle(_context));
	}

	Task<IConnectionState> IConnectionState.Toggle() => Disconnect().ContinueWith<IConnectionState>(t => t.Result);

	Task<IConnectionState> IConnectionState.OnTimerFired(int timerId) => Task.FromResult<IConnectionState>(this);

	Task<IConnectionState> IConnectionState.OnServiceUnavailable() => Task.FromResult(_context.RetryAfterDelay());
}

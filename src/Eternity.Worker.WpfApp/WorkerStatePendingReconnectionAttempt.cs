namespace Eternity.Worker.WpfApp
{
	using System.Threading.Tasks;

	internal class WorkerStatePendingReconnectionAttempt(WorkerStateContext context, int timerId) : WorkerState
	{
		Task<WorkerState> WorkerState.OnServiceUnavailable() => Task.FromResult<WorkerState>(this);

		Task<WorkerState> WorkerState.OnTimerFired(int timerId2) =>
			timerId2 switch
			{
				int n when n == timerId => Task.FromResult(context.AttemptConnection()),
				_ => Task.FromResult<WorkerState>(this)
			};

		Task<WorkerState> WorkerState.Toggle() => Task.FromResult<WorkerState>(this);
	}
}

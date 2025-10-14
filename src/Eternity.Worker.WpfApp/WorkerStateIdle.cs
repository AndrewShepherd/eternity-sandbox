namespace Eternity.Worker.WpfApp;

sealed class WorkerStateIdle(WorkerStateContext context) : WorkerState
{

	Task<WorkerState> WorkerState.OnTimerFired(int timerId) => Task.FromResult<WorkerState>(this);
	public Task<WorkerState> Connect() => Task.FromResult(context.AttemptConnection());

	Task<WorkerState> WorkerState.Toggle() => Connect();

	Task<WorkerState> WorkerState.OnServiceUnavailable() => Task.FromResult<WorkerState>(this);
}

namespace Eternity.Worker.WpfApp
{
	abstract record class WorkerState
	{
	}

	record class WorkerStateIdle() : WorkerState;

	record class WorkerStateWorking(CancellationTokenSource CancellationTokenSource) : WorkerState;
}

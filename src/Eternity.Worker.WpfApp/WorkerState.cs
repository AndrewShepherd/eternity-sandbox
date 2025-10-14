namespace Eternity.Worker.WpfApp;

interface WorkerState
{
	public Task<WorkerState> OnTimerFired(int timerId);

	public Task<WorkerState> OnServiceUnavailable(); 

	public Task<WorkerState> Toggle();
}

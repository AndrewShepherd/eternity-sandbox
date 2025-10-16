namespace Eternity.Worker.WpfApp;

interface IConnectionState
{
	public Task<IConnectionState> OnTimerFired(int timerId);

	public Task<IConnectionState> OnServiceUnavailable(); 

	public Task<IConnectionState> Toggle();
}

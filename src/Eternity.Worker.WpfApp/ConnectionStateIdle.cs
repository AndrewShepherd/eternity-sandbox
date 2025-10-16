namespace Eternity.Worker.WpfApp;

sealed class ConnectionStateIdle(ConnectionStateContext context) : IConnectionState
{

	Task<IConnectionState> IConnectionState.OnTimerFired(int timerId) => Task.FromResult<IConnectionState>(this);
	public Task<IConnectionState> Connect() => Task.FromResult(context.AttemptConnection());

	Task<IConnectionState> IConnectionState.Toggle() => Connect();

	Task<IConnectionState> IConnectionState.OnServiceUnavailable() => Task.FromResult<IConnectionState>(this);
}

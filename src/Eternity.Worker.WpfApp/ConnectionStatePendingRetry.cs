namespace Eternity.Worker.WpfApp
{
	using System.Threading.Tasks;

	internal class ConnectionStatePendingRetry(ConnectionStateContext context, int timerId) : IConnectionState
	{
		Task<IConnectionState> IConnectionState.OnServiceUnavailable() => Task.FromResult<IConnectionState>(this);

		Task<IConnectionState> IConnectionState.OnTimerFired(int timerId2) =>
			timerId2 switch
			{
				int n when n == timerId => Task.FromResult(context.AttemptConnection()),
				_ => Task.FromResult<IConnectionState>(this)
			};

		Task<IConnectionState> IConnectionState.Toggle() => Task.FromResult<IConnectionState>(this);
	}
}

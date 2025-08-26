namespace Eternity.Server.WpfApp
{
	using Eternity.Proto;
	using Grpc.Core;
	using System.Collections.Concurrent;
	using System.Reactive.Linq;
	using System.Reactive.Subjects;
	using static Eternity.Proto.EternityService;

	public sealed class ConnectionEntry
	{
		public required IAsyncStreamReader<MessageToServer> RequestStream { get; init; }
		public required IServerStreamWriter<MessageToWorker> ResponseStream { get; init; }

		public required TaskCompletionSource CompletionTask { get; init; }

		public required CancellationTokenSource CancellationTokenSource { get; init; }
	}

	sealed class EternityServiceImpl : EternityServiceBase
	{

		private ConcurrentDictionary<ConnectionEntry, bool> _connections = new();
		private BehaviorSubject<IReadOnlyList<ConnectionEntry>> _connectionEntrySubject = new([]);

		internal IObservable<IReadOnlyList<ConnectionEntry>> Connections => _connectionEntrySubject.AsObservable();

		public EternityServiceImpl()
		{
		}

		private void UpdateObservables()
		{
			_connectionEntrySubject.OnNext(_connections.Keys.ToList());
		}

		public override Task ConnectWorker(
			IAsyncStreamReader<MessageToServer> requestStream,
			IServerStreamWriter<MessageToWorker> responseStream,
			ServerCallContext context
		)
		{
			var completionTask = new TaskCompletionSource();
			ConnectionEntry connection = new()
			{
				RequestStream = requestStream,
				ResponseStream = responseStream,
				CompletionTask = completionTask,
				CancellationTokenSource = new(),
			};
			_connections.TryAdd(
				connection,
				true
			);
			UpdateObservables();
			Task.Run(
				async () =>
				{
					while(await requestStream.MoveNext(connection.CancellationTokenSource.Token))
					{
						var message = requestStream.Current;
					}
					_connections.TryRemove(
						connection,
						out var _
					);
					UpdateObservables();
					connection.CompletionTask.SetResult();
				}
			);
			return completionTask.Task;
		}
	}
}

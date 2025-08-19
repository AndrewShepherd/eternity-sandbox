namespace Eternity.Server.WpfApp
{
	using Eternity.Proto;
	using Grpc.Core;
	using System.Collections.Concurrent;
	using static Eternity.Proto.EternityService;

	sealed class ConnectionEntry
	{
		public required IAsyncStreamReader<MessageToServer> RequestStream { get; init; }
		public required IServerStreamWriter<MessageToWorker> ResponseStream { get; init; }

		public required TaskCompletionSource CompletionTask { get; init; }
	}

	sealed class EternityServiceImpl : EternityServiceBase
	{

		private ConcurrentDictionary<ConnectionEntry, bool> _connections = new();

		public override Task ConnectWorker(
			IAsyncStreamReader<MessageToServer> requestStream,
			IServerStreamWriter<MessageToWorker> responseStream,
			ServerCallContext context
		)
		{
			var completionTask = new TaskCompletionSource();
			_connections.TryAdd(
				new()
				{ 
					RequestStream = requestStream,
					ResponseStream = responseStream,
					CompletionTask = completionTask,
				},
				true
			);
			return completionTask.Task;
		}
	}
}

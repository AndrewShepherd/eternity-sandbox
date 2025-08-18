namespace Eternity.Server.WpfApp
{
	using Eternity.Proto;
	using Grpc.Core;
	using static Eternity.Proto.EternityService;
	sealed class EternityServiceImpl : EternityServiceBase
	{
		public override Task ConnectWorker(
			IAsyncStreamReader<MessageToServer> requestStream,
			IServerStreamWriter<MessageToWorker> responseStream,
			ServerCallContext context
		)
		{
			return base.ConnectWorker(requestStream, responseStream, context);
		}
	}
}

using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Eternity.Proto.EternityService;

namespace Eternity.Worker.WpfApp
{
	internal static class WorkerStateContextExtensions
	{
		static readonly TimeSpan _retryTimespan = TimeSpan.FromSeconds(1);

		internal static WorkerState RetryAfterDelay(this WorkerStateContext context) =>
			new WorkerStatePendingReconnectionAttempt(
						context,
						context.SetTimer(_retryTimespan)
				);

		internal static WorkerState AttemptConnection(this WorkerStateContext context)
		{
			try
			{
				var channel = new Channel(
					"localhost",
					3876,
					ChannelCredentials.Insecure
				);
				var client = new EternityServiceClient(channel);
				// How do you know if this was successful or not?
				var call = client.ConnectWorker();
				var connectedWorkerState = new WorkerStateConnected(context, call);
				connectedWorkerState.Listen();
				return connectedWorkerState;
			}
			catch (Exception)
			{
				return context.RetryAfterDelay();
			}
		}
	}
}

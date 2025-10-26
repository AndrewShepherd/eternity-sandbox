using System;
using System.Collections.Generic;
using System.Linq;


namespace Eternity.Worker.WpfApp
{
	using Eternity.Proto;
	using Grpc.Core;
	using ReactiveUI;
	using System.Runtime.CompilerServices;
	using System.Text;
	using System.Reactive.Linq;
	using System.Threading.Channels;
	using System.Threading.Tasks;
	using static Eternity.Proto.EternityService;

	internal static class MyObservableExtensions
	{
		internal static ChannelReader<T> ToChannel<T>(this IObservable<T> obs)
		{
			var channel = System.Threading.Channels.Channel.CreateUnbounded<T>();
			obs.Subscribe(
				t => channel.Writer.WriteAsync(t),
				() => channel.Writer.Complete()
			);
			return channel.Reader;
		}
	}

	internal static class ConnectionStateContextExtensions
	{
		static readonly TimeSpan _retryTimespan = TimeSpan.FromSeconds(1);

		internal static IConnectionState RetryAfterDelay(this ConnectionStateContext context) =>
			new ConnectionStatePendingRetry(
						context,
						context.SetTimer(_retryTimespan)
				);

		private static MessageToServer ConvertToServerMessage(Eternity.TreeNode treeNode, string jobId) =>
			new()
			{
				WorkProgress = new()
				{
					JobId = jobId,
					Tree = ProtoTreeNodeConversions.Convert(treeNode),
				}
			};

		private static void ProcessIncomingMessage(
			ConnectionStateContext context,
			MessageToWorker message
		)
		{
			if (message.MessageContentsCase == MessageToWorker.MessageContentsOneofCase.WorkInstruction)
			{
				var workInstruction = message.WorkInstruction;
				var solutionState = SolutionStateProto.Convert(workInstruction.RunningState);

				var treeNodes = context.SetWork(solutionState, workInstruction.InitialPath);

				var messagesToSendBack = treeNodes.Select(
					tn => ConvertToServerMessage(tn, workInstruction.JobId)
				);
				context.SetReturnMessages(messagesToSendBack);
			}
		}

		private static async void HandleIncomingMessages(
			ConnectionStateContext context,
			IObservable<Proto.MessageToWorker> incoming
		)
		{
			var r = incoming.ToChannel();
			while(await r.WaitToReadAsync())
			{
				while (r.TryRead(out var message))
				{
					ProcessIncomingMessage(context, message);
				}
			}
		}

		internal static IConnectionState AttemptConnection(this ConnectionStateContext context)
		{
			try
			{
				var channel = new Grpc.Core.Channel(
					"localhost",
					3876,
					ChannelCredentials.Insecure
				);
				var client = new EternityServiceClient(channel);
				// How do you know if this was successful or not?
				var call = client.ConnectWorker();
				var connectedWorkerState = new ConnectionStateConnected(context, call);
				var observable = connectedWorkerState.Listen();
				HandleIncomingMessages(context, observable);
				return connectedWorkerState;
			}
			catch (Exception)
			{
				return context.RetryAfterDelay();
			}
		}
	}
}

using System.Collections.Immutable;

namespace Eternity.Server.WpfApp;

internal sealed class ServerJob
{
	private SolutionState _solutionState;
	private ImmutableList<ConnectionEntry> _workers;
	private readonly string _jobId = Guid.NewGuid().ToString();
	private ServerJob(SolutionState solutionState, IEnumerable<ConnectionEntry> workers)
	{
		_solutionState = solutionState;
		_workers = ImmutableList<ConnectionEntry>.Empty.AddRange(workers);
	}

	private static Proto.WorkInstruction CreateWorkInstruction(
		string jobId,
		SolutionState solutionState,
		IEnumerable<int> path
	)
	{
		Proto.WorkInstruction workInstruction = new()
		{
			JobId = jobId,
			RunningState = SolutionStateProto.Convert(solutionState),
		};
		workInstruction.InitialPath.AddRange(path);
		return workInstruction;
	}

	public static async Task<ServerJob> Start(
		SolutionState solutionState,
		IEnumerable<ConnectionEntry> connections
	)
	{
		var serverJob = new ServerJob(solutionState, connections);
		// TODO:
		//  Listen to workers

		(solutionState._treeNode, var jobs) = Job.DivideIntoJobs(solutionState._treeNode, connections.Count());
		foreach (var (job, connection) in jobs.Zip(connections))
		{
			await connection.ResponseStream.WriteAsync(
				new Proto.MessageToWorker
				{
					WorkInstruction = CreateWorkInstruction(
						serverJob._jobId,
						solutionState,
						job
					),
				}
			);
		}
		// Plenty more to do. Must deal with the case where there are still idle workers
		return serverJob;
	}
}

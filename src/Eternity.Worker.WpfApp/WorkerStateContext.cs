namespace Eternity.Worker.WpfApp
{
	using System.Threading;

	abstract record WorkerStateContextEvent();

	sealed record TimerFiredEvent(int timerId) : WorkerStateContextEvent;

	sealed record ServiceUnavailableEvent : WorkerStateContextEvent;

	internal class WorkerStateContext
	{
		System.Reactive.Subjects.Subject<WorkerStateContextEvent> _events = new();
		public IObservable<WorkerStateContextEvent> Events => _events;

		private int _timerId = 0;
		public int SetTimer(TimeSpan length)
		{
			int thisTimerId = Interlocked.Increment(ref _timerId);
			Task.Delay(length).ContinueWith(_ => FireEvent(new TimerFiredEvent(thisTimerId)));
			return thisTimerId;
		}

		public void FireEvent(WorkerStateContextEvent e) => _events.OnNext(e);
	}
}

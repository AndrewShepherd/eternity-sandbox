namespace Eternity.Worker.WpfApp
{
	using System.Reactive.Linq;
	using System.Reactive.Subjects;
	using System.Threading;

	abstract record ConnectionStateEvent();

	sealed record TimerFiredEvent(int timerId) : ConnectionStateEvent;

	sealed record ServiceUnavailableEvent : ConnectionStateEvent;

	internal class ConnectionStateContext
	{
		System.Reactive.Subjects.Subject<ConnectionStateEvent> _events = new();
		public IObservable<ConnectionStateEvent> Events => _events;

		private int _timerId = 0;
		public int SetTimer(TimeSpan length)
		{
			int thisTimerId = Interlocked.Increment(ref _timerId);
			Task.Delay(length).ContinueWith(_ => FireEvent(new TimerFiredEvent(thisTimerId)));
			return thisTimerId;
		}

		public void FireEvent(ConnectionStateEvent e) => _events.OnNext(e);


		private readonly BehaviorSubject<IObservable<Placements>> _placementsSubject = new(
			Observable.Return(Eternity.Placements.None)
		);

		internal void SetPlacementsSource(IObservable<Placements> source) => _placementsSubject.OnNext(source);
		public IObservable<Placements> Placements => _placementsSubject.SelectMany(_ => _);
	}
}

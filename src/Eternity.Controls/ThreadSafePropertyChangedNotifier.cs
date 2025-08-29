namespace Eternity.Controls;

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Windows.Threading;

public sealed class ThreadSafePropertyChangedNotifier(Action<PropertyChangedEventArgs> handle)
{
	private readonly Dispatcher _uiDispatcher = Dispatcher.CurrentDispatcher;

	private ConcurrentDictionary<string, bool> _staleProps = new();

	private static int _collisionCount = 0;

	public void PropertyChanged(string propertyName)
	{
		if (_staleProps.TryAdd(propertyName, true))
		{
			_uiDispatcher.BeginInvoke(
				() =>
				{
					while (_staleProps.Count > 0)
					{
						foreach (var k in _staleProps.Keys)
						{
							if (_staleProps.TryRemove(k, out bool dummy))
							{
								handle(new PropertyChangedEventArgs(k));
							}
						}
					}
				}
			);
		} else
		{
			_collisionCount++;
		}
	}

}

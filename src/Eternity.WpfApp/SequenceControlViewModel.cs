using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Eternity.WpfApp
{
	record class ValueAndDate(int value, DateTime date);

	public class SequenceControlViewModel : INotifyPropertyChanged
	{
		private IReadOnlyList<int> _sequence = new SequenceSpecs(256).GenerateFirst();

		private IReadOnlyList<ValueAndDate>? _valuesAndDates = null;


		private static Color ConvertToForegroundColor(TimeSpan timeSpan)
		{
			if (timeSpan.TotalMinutes >= 1.0)
			{
				return Colors.Black;
			}
			var milliseconds = timeSpan.TotalMilliseconds;
			// 0 = 255
			// 60000 = 0

			var converted = (byte)255 - (byte)(255 * milliseconds / 60000.0);
			return new Color
			{
				A = 255,
				B = 0,
				G = 0,
				R = (byte)converted
			};
		}
		public IReadOnlyList<int> Sequence
		{
			get => _sequence;
			set
			{
				if (_sequence != value)
				{
					if (_valuesAndDates != null && value.Count != _valuesAndDates.Count)
					{
						_valuesAndDates = null;
					}
					_sequence = value;
					DateTime dateTimeNow = DateTime.Now;
					if (_valuesAndDates == null)
					{
						var d = DateTime.MinValue;
						_valuesAndDates = _sequence.Select(
							n =>
							new ValueAndDate(n, d)
						).ToArray();
					}
					else
					{
						_valuesAndDates = _sequence.Zip(
							_valuesAndDates,
							(s, v) =>
							{
								if (s == v.value)
								{
									return v;
								}
								else
								{
									return new ValueAndDate(s, dateTimeNow);
								}
							}
						).ToArray();
					}

					
					for (int i = 0; i < _sequence.Count; i++)
					{
						if (i >= _valuesAndDates.Count)
						{
							break;
						}
						var age = (DateTime.Now - _valuesAndDates[i].date);
						if (i >= this.SequenceListEntries.Count)
						{
							this.SequenceListEntries.Add(
								new()
								{
									ForegroundColor = ConvertToForegroundColor(age),
									Value = _sequence[i]
								}
							);
						}
						else
						{
							this.SequenceListEntries[i].Value = _sequence[i];
							this.SequenceListEntries[i].ForegroundColor = ConvertToForegroundColor(age);
						}
					}
					while(this.SequenceListEntries.Count> _sequence.Count)
					{
						this.SequenceListEntries.RemoveAt(this.SequenceListEntries.Count - 1);
					}
					_notifier.PropertyChanged(nameof(Sequence));
				}
			}
		}

		private readonly ThreadSafePropertyChangedNotifier _notifier;

		public SequenceControlViewModel()
		{
			_notifier = new(n => _propertyChanged?.Invoke(this, n));
		}

		private int _selectedSequenceIndex = -1;
		public int SelectedSequenceIndex
		{
			get => _selectedSequenceIndex;
			set
			{
				if (_selectedSequenceIndex != value)
				{
					_selectedSequenceIndex = value;
					_propertyChanged?.Invoke(this, new(nameof(SelectedSequenceIndex)));
				}
			}
		}

		private PropertyChangedEventHandler? _propertyChanged;
		event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
		{
			add => _propertyChanged += value;

			remove => _propertyChanged -= value;
		}

		public ObservableCollection<SequenceListEntry> SequenceListEntries { get; set; } =
			new ObservableCollection<SequenceListEntry>(
				Enumerable.Range(0, 256).Select(d => new SequenceListEntry { Value = 0 })
			);

	}
}

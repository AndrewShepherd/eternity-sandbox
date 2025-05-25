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
	record class ValueAndDate(StackEntry value, DateTime date);

	public class SequenceControlViewModel : INotifyPropertyChanged
	{
		private IReadOnlyList<StackEntry> _stackEntries = [];

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
		public IReadOnlyList<StackEntry> StackEntries
		{
			get => _stackEntries;
			set
			{
				if (_stackEntries != value)
				{
					_stackEntries = value;
					DateTime dateTimeNow = DateTime.Now;
					if (_valuesAndDates == null)
					{
						var d = DateTime.MinValue;
						_valuesAndDates = _stackEntries.Select(
							n =>
							new ValueAndDate(n, d)
						).ToArray();
					}
					else
					{
						_valuesAndDates = _stackEntries.Zip(
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

					var newValues = new List<ValueAndDate>();
					for (int i = _valuesAndDates.Count; i < _stackEntries.Count; i++)
					{
						newValues.Add(new ValueAndDate(_stackEntries[i], dateTimeNow));	
					}
					if (newValues.Count > 0)
					{
						_valuesAndDates = _valuesAndDates.Concat(newValues).ToArray();
					}
					var firstPlacements = _stackEntries[0].Placements;
					int expectedCount = 0;
					if (firstPlacements != null)
					{
						expectedCount = firstPlacements.Dimensions.Width * firstPlacements.Dimensions.Height;
					}
					if (this.SequenceListEntries.Count != expectedCount)
					{
						this.SequenceListEntries.Clear();
					}
					for (int i = 0; i < _stackEntries.Count; i++)
					{
						if (i >= _valuesAndDates.Count)
						{
							break;
						}
						var age = (DateTime.Now - _valuesAndDates[i].date);
						if (i >= this.SequenceListEntries.Count)
						{
							this.SequenceListEntries.Add(
								new(_stackEntries[i])
								{
									ForegroundColor = ConvertToForegroundColor(age),
								}
							);
						}
						else
						{
							this.SequenceListEntries[i].Value = _stackEntries[i];
							this.SequenceListEntries[i].ForegroundColor = ConvertToForegroundColor(age);
						}
					}
					for (int i = _stackEntries.Count; i < expectedCount; ++i)
					{
						if (i >= this.SequenceListEntries.Count)
						{
							this.SequenceListEntries.Add(
								new(null)
								{
									ForegroundColor = Colors.LightGray
								}
							);
						}
						else
						{
							this.SequenceListEntries[i].Value = null;
							this.SequenceListEntries[i].ForegroundColor = Colors.LightGray;
						}
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
			);

	}
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternity.WpfApp
{
	public class SequenceControlViewModel : INotifyPropertyChanged
	{
		private IReadOnlyList<int> _sequence = Eternity.Sequence.FirstSequence;
		public IReadOnlyList<int> Sequence
		{
			get => _sequence;
			set
			{
				if (_sequence != value)
				{
					_sequence = value;
					for (int i = 0; i < _sequence.Count; i++)
					{
						this.SequenceListEntries[i].Value = _sequence[i];
					}
					_propertyChanged?.Invoke(this, new(nameof(Sequence)));
				}
			}
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
				Eternity.Sequence.Dimensions.Select(d => new SequenceListEntry { Value = 0 })
			);

	}
}

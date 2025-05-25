namespace Eternity.WpfApp
{
	using System.ComponentModel;
	using System.Windows.Media;

	public class SequenceListEntry : INotifyPropertyChanged
	{
		public SequenceListEntry(StackEntry? stackEntry)
		{
			_value = stackEntry;
		}
		private PropertyChangedEventHandler? _propertyChanged;
		event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
		{
			add => _propertyChanged += value;
			remove => _propertyChanged -= value;
		}

		private StackEntry? _value;
		public StackEntry? Value
		{
			get => _value;
			set
			{
				if (value != _value)
				{
					_value = value;
					_propertyChanged?.Invoke(this, new(nameof(Value)));
					_propertyChanged?.Invoke(this, new(nameof(AsFraction)));
				}
			}
		}

		private Color _foregroundColor = Colors.Black;
		public Color ForegroundColor
		{
			get => _foregroundColor;
			set
			{
				if (value != _foregroundColor)
				{
					_foregroundColor = value;
					_propertyChanged?.Invoke(this, new(nameof(ForegroundColor)));
				}
			}
		}

		public string AsFraction => 
			this.Value == null
				? "_/_" 
				: $"{this.Value.PieceIndex}/{this.Value.PossiblePieceCount}";
	}
}

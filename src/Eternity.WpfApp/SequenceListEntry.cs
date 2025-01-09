namespace Eternity.WpfApp
{
	using System.ComponentModel;
	using System.Windows.Media;

	public class SequenceListEntry : INotifyPropertyChanged
	{
		private PropertyChangedEventHandler? _propertyChanged;
		event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
		{
			add => _propertyChanged += value;
			remove => _propertyChanged -= value;
		}

		private int _value;
		public int Value
		{
			get => _value;
			set
			{
				if (value != _value)
				{
					_value = value;
					_propertyChanged?.Invoke(this, new(nameof(Value)));
					_propertyChanged?.Invoke(this, new(nameof(ValueAsHex)));
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

		public string ValueAsHex => AsTwoDigitHex(_value);

		private static string AsTwoDigitHex(int n)
		{
			var unpaddedHex = $"{n:X}";
			return unpaddedHex.Length == 1 ? $"0{unpaddedHex}" : unpaddedHex;
		}
		public static string SequenceToString(IEnumerable<int> sequence) =>
			string.Join(' ', sequence.Select(AsTwoDigitHex));

		public override string ToString() => AsTwoDigitHex(_value);
	}
}

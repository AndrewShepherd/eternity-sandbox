namespace Eternity.WpfApp
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	// Converter for radio button binding
	public static class SelectionToBooleanConverter
	{
		class SbcImpl<T>(T targetSelection) : IValueConverter where T : struct
		{
			object IValueConverter.Convert(object value, Type _1, object _2, CultureInfo _3) =>
				(value is T selection) && selection.Equals(targetSelection);

			object IValueConverter.ConvertBack(object value, Type _1, object _2, CultureInfo _3) =>
				value switch
				{
					true => targetSelection,
					_ => Binding.DoNothing
				};
		}
		public static IValueConverter Create<T>(T value) where T :struct => new SbcImpl<T>(value);
	}
} 
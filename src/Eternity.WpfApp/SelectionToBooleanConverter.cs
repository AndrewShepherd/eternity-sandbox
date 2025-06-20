namespace Eternity.WpfApp
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	// Converter for radio button binding
	public static class SelectionToBooleanConverter
	{
		class SbcImpl<T>(T targetSelection) : IValueConverter where T : struct/* Enum*/
		{
			object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
				(value is T selection) && selection.Equals(targetSelection);

			object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
				value switch
				{
					true => targetSelection,
					_ => Binding.DoNothing
				};
		}
		public static IValueConverter Create<T>(T value) where T :struct /*, Enum*/ =>
			new SbcImpl<T>(value);
	}
} 
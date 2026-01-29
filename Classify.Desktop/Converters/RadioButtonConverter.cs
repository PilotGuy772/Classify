using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Classify.Desktop.Converters;

public class EnumEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (bool)value! ? Enum.Parse(targetType, parameter!.ToString()!) : Avalonia.Data.BindingOperations.DoNothing;
}
using System;
using System.Globalization;
using System.Windows.Data;

namespace Issue1585.Converters;

// A hand-written IValueConverter (no aspect on it). The repro's cascading MC3074 error
// will claim this class "does not exist in XML namespace" because the temp compile
// fails on SwapTransformPackage.IsChanged/AcceptChanges/RejectChanges, preventing the
// XAML compiler from finding any type in the temp assembly.
public class EnumToStringConverter : IValueConverter
{
    public object? Convert( object? value, Type targetType, object? parameter, CultureInfo culture )
        => value?.ToString();

    public object? ConvertBack( object? value, Type targetType, object? parameter, CultureInfo culture )
        => throw new NotSupportedException();
}

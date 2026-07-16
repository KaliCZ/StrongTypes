#nullable enable

using System.Globalization;
using System.Windows.Data;

namespace StrongTypes.Wpf.Tests;

internal static class Bindings
{
    public static Binding OneWay(string path, object source) => new(path)
    {
        Source = source,
        Mode = BindingMode.OneWay,
    };

    public static Binding TwoWay(string path, object source, CultureInfo? culture = null) => new(path)
    {
        Source = source,
        Mode = BindingMode.TwoWay,
        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        ValidatesOnExceptions = true,
        NotifyOnValidationError = true,
        ConverterCulture = culture,
    };
}

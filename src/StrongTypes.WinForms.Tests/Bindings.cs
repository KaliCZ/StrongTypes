#nullable enable

using System.Globalization;
using System.Windows.Forms;

namespace StrongTypes.WinForms.Tests;

internal static class Bindings
{
    public static Binding TwoWay(string path, object source, CultureInfo? culture = null) =>
        new("Text", source, path, formattingEnabled: true, DataSourceUpdateMode.OnPropertyChanged) { FormatInfo = culture };
}

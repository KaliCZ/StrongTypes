#nullable enable

using System.Runtime.CompilerServices;
using System.Windows;

namespace StrongTypes.Wpf.Tests;

internal static class TestSetup
{
    // No strong-type registration: binding must work off the [TypeConverter] the
    // core package puts on each type. An Application instance is still required
    // for WPF's binding engine to resolve resources.
    [ModuleInitializer]
    internal static void Init() => StaThread.Run(() => new Application());
}

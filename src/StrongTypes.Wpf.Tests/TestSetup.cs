#nullable enable

using System.Runtime.CompilerServices;
using System.Windows;

namespace StrongTypes.Wpf.Tests;

internal static class TestSetup
{
    // An Application instance is required for WPF's binding engine to resolve resources.
    [ModuleInitializer]
    internal static void Init() => StaThread.Run(() => new Application());
}

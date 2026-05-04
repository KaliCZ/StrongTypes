#nullable enable

using System.Runtime.CompilerServices;
using System.Windows;

namespace StrongTypes.Wpf.Tests;

internal static class TestSetup
{
    [ModuleInitializer]
    internal static void Init() => StaThread.Run(() => new Application().UseStrongTypes());
}

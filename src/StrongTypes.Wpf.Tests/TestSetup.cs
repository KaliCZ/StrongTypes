#nullable enable

using System.Runtime.CompilerServices;

namespace StrongTypes.Wpf.Tests;

internal static class TestSetup
{
    [ModuleInitializer]
    internal static void Init() => StrongTypesWpf.Register();
}

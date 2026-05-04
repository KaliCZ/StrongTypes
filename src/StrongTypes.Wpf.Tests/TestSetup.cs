#nullable enable

using System.Runtime.CompilerServices;

namespace StrongTypes.Wpf.Tests;

internal static class TestSetup
{
    [ModuleInitializer]
    internal static void Init()
    {
        StrongTypes.Wpf.StrongTypesWpf.Register();
        StrongTypes.Wpf.StrongTypesWpf.Register<Positive<int>>();
    }
}

using System.Windows;

namespace StrongTypes.Wpf;

public static class ApplicationExtensions
{
    /// <summary>Wires <see cref="ParsableTypeConverter{T}"/> into <see cref="System.ComponentModel.TypeDescriptor"/> for every strong type shipped in <c>Kalicz.StrongTypes</c>, enabling two-way MVVM binding from <c>TextBox.Text</c> to strong-typed view-model properties. Call once from <c>App.OnStartup</c>. Idempotent.</summary>
    /// <returns><paramref name="application"/>, for fluent chaining.</returns>
    public static Application UseStrongTypes(this Application application)
    {
        StrongTypesWpf.Register();
        return application;
    }
}

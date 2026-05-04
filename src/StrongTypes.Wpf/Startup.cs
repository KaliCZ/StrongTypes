using System.ComponentModel;
using System.Windows;

namespace StrongTypes.Wpf;

public static class ApplicationStartupExtensions
{
    private static readonly object _gate = new();
    private static bool _registered;

    /// <summary>Wires <see cref="ParsableTypeConverter{T}"/> into <see cref="TypeDescriptor"/> for every strong type shipped in <c>Kalicz.StrongTypes</c>, enabling two-way MVVM binding from <c>TextBox.Text</c> to strong-typed view-model properties. Call once from <c>App.OnStartup</c>. Idempotent.</summary>
    /// <returns><paramref name="application"/>, for fluent chaining.</returns>
    public static Application UseStrongTypes(this Application application)
    {
        if (_registered)
            return application;

        lock (_gate)
        {
            if (_registered)
                return application;

            var parent = TypeDescriptor.GetProvider(typeof(object));
            TypeDescriptor.AddProvider(new StrongTypesTypeDescriptionProvider(parent), typeof(object));
            _registered = true;
        }

        return application;
    }
}

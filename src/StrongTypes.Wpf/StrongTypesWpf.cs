using System.ComponentModel;
using System.Windows;

namespace StrongTypes.Wpf;

/// <summary>Wires <see cref="ParsableTypeConverter{T}"/> into <see cref="TypeDescriptor"/> so WPF (and any other framework that resolves converters through <see cref="TypeDescriptor.GetConverter(System.Type)"/>) can convert a string to a strong type during two-way MVVM binding.</summary>
public static class StrongTypesWpf
{
    private static readonly object _gate = new();
    private static bool _registered;

    /// <summary>Wires <see cref="ParsableTypeConverter{T}"/> into <see cref="TypeDescriptor"/> for every strong type shipped in <c>Kalicz.StrongTypes</c>, enabling two-way MVVM binding from <c>TextBox.Text</c> to strong-typed view-model properties. Call once from <c>App.OnStartup</c>. Idempotent.</summary>
    /// <returns><paramref name="application"/>, for fluent chaining.</returns>
    public static Application UseStrongTypes(this Application application)
    {
        Register();
        return application;
    }

    /// <summary>Installs a <see cref="TypeDescriptionProvider"/> that synthesizes a <see cref="ParsableTypeConverter{T}"/> on demand for every strong type shipped in <c>Kalicz.StrongTypes</c>: <see cref="NonEmptyString"/>, <see cref="Email"/>, <see cref="Digit"/>, and every closed instantiation of <see cref="Positive{T}"/> / <see cref="NonNegative{T}"/> / <see cref="Negative{T}"/> / <see cref="NonPositive{T}"/>. Idempotent. Most apps should call <see cref="UseStrongTypes"/> from <c>App.OnStartup</c> instead; this entry point exists for non-WPF callers (tests, console hosts).</summary>
    public static void Register()
    {
        if (_registered)
            return;

        lock (_gate)
        {
            if (_registered)
                return;
            var parent = TypeDescriptor.GetProvider(typeof(object));
            TypeDescriptor.AddProvider(new StrongTypesTypeDescriptionProvider(parent), typeof(object));
            _registered = true;
        }
    }
}

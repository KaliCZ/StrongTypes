using System;
using System.ComponentModel;

namespace StrongTypes.Wpf;

/// <summary>Wires <see cref="ParsableTypeConverter{T}"/> into <see cref="TypeDescriptor"/> so WPF (and any other framework that resolves converters through <see cref="TypeDescriptor.GetConverter(System.Type)"/>) can convert a string to a strong type during two-way MVVM binding.</summary>
public static class StrongTypesWpf
{
    private static readonly object _gate = new();
    private static bool _registered;

    /// <summary>Installs a <see cref="TypeDescriptionProvider"/> that synthesizes a <see cref="ParsableTypeConverter{T}"/> on demand for every strong type shipped in <c>Kalicz.StrongTypes</c>: <see cref="NonEmptyString"/>, <see cref="Email"/>, <see cref="Digit"/>, and every closed instantiation of <see cref="Positive{T}"/> / <see cref="NonNegative{T}"/> / <see cref="Negative{T}"/> / <see cref="NonPositive{T}"/>. Idempotent. Most apps should call <see cref="ApplicationExtensions.UseStrongTypes"/> from <c>App.OnStartup</c> instead; this entry point exists for non-WPF callers (tests, console hosts).</summary>
    public static void Register()
    {
        lock (_gate)
        {
            if (_registered)
                return;
            var parent = TypeDescriptor.GetProvider(typeof(object));
            TypeDescriptor.AddProvider(new StrongTypesTypeDescriptionProvider(parent), typeof(object));
            _registered = true;
        }
    }

    /// <summary>Registers a <see cref="ParsableTypeConverter{T}"/> for a single <typeparamref name="T"/> not covered by <see cref="Register"/> — e.g. a custom <see cref="IParsable{T}"/> in your own code. Idempotent.</summary>
    /// <typeparam name="T">A type that implements <see cref="IParsable{T}"/>.</typeparam>
    public static void Register<T>() where T : IParsable<T>
    {
        TypeDescriptor.AddAttributes(typeof(T), new TypeConverterAttribute(typeof(ParsableTypeConverter<T>)));
    }
}

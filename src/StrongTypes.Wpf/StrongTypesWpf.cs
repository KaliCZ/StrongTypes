using System;
using System.ComponentModel;

namespace StrongTypes.Wpf;

/// <summary>Wires <see cref="ParsableTypeConverter{T}"/> into <see cref="TypeDescriptor"/> so WPF (and any other framework that resolves converters through <see cref="TypeDescriptor.GetConverter(System.Type)"/>) can convert a string to a strong type during two-way binding.</summary>
public static class StrongTypesWpf
{
    /// <summary>Registers a <see cref="ParsableTypeConverter{T}"/> for <typeparamref name="T"/>. Idempotent.</summary>
    /// <typeparam name="T">A strong type that implements <see cref="IParsable{T}"/>.</typeparam>
    public static void Register<T>() where T : IParsable<T>
    {
        TypeDescriptor.AddAttributes(typeof(T), new TypeConverterAttribute(typeof(ParsableTypeConverter<T>)));
    }

    /// <summary>Registers converters for the non-generic strong types shipped in <c>StrongTypes</c>: <see cref="NonEmptyString"/>, <see cref="Email"/>, <see cref="Digit"/>. Generic numeric wrappers must be registered per closed instantiation via <see cref="Register{T}"/> — e.g. <c>Register&lt;Positive&lt;int&gt;&gt;()</c>.</summary>
    public static void Register()
    {
        Register<NonEmptyString>();
        Register<Email>();
        Register<Digit>();
    }
}

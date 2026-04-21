#nullable enable

using System;

namespace StrongTypes;

public static class NullableMapToStructExtensions
{
    /// <summary>
    /// Applies <paramref name="map"/> to the value when present and returns its
    /// result; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>.
    /// </summary>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult> map)
        where T : struct
        where TResult : struct
        => value.HasValue ? map(value.Value) : null;

    /// <summary>
    /// Applies <paramref name="map"/> to the value when present and returns its
    /// result; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>. The
    /// mapper may itself return <see langword="null"/>.
    /// </summary>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult?> map)
        where T : struct
        where TResult : struct
        => value.HasValue ? map(value.Value) : null;

    /// <summary>
    /// Applies <paramref name="map"/> to the value when present and returns its
    /// result; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>.
    /// </summary>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult> map)
        where T : class
        where TResult : struct
        => value is not null ? map(value) : null;

    /// <summary>
    /// Applies <paramref name="map"/> to the value when present and returns its
    /// result; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>. The
    /// mapper may itself return <see langword="null"/>.
    /// </summary>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult?> map)
        where T : class
        where TResult : struct
        => value is not null ? map(value) : null;
}

public static class NullableMapToClassExtensions
{
    /// <summary>
    /// Applies <paramref name="map"/> to the value when present and returns the
    /// result; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>. The
    /// mapper may itself return <see langword="null"/>.
    /// </summary>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult?> map)
        where T : struct
        where TResult : class
        => value.HasValue ? map(value.Value) : null;

    /// <summary>
    /// Applies <paramref name="map"/> to the value when present and returns the
    /// result; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>. The
    /// mapper may itself return <see langword="null"/>.
    /// </summary>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult?> map)
        where T : class
        where TResult : class
        => value is not null ? map(value) : null;
}

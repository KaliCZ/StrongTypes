using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace StrongTypes;

public static class NullableMapToStructExtensions
{
    /// <summary>
    /// Applies <paramref name="map"/> to the value when present and returns its
    /// result; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
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
    [return: NotNullIfNotNull(nameof(value))]
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

    /// <summary>
    /// Awaits and returns the result of <paramref name="map"/> when the value
    /// is present; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult>> map)
        where T : struct
        where TResult : struct
        => value.HasValue ? await map(value.Value) : null;

    /// <summary>
    /// Awaits and returns the result of <paramref name="map"/> when the value
    /// is present; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>. The
    /// mapper may itself return <see langword="null"/>.
    /// </summary>
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult?>> map)
        where T : struct
        where TResult : struct
        => value.HasValue ? await map(value.Value) : null;

    /// <summary>
    /// Awaits and returns the result of <paramref name="map"/> when the value
    /// is present; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(value))]
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult>> map)
        where T : class
        where TResult : struct
        => value is not null ? await map(value) : null;

    /// <summary>
    /// Awaits and returns the result of <paramref name="map"/> when the value
    /// is present; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>. The
    /// mapper may itself return <see langword="null"/>.
    /// </summary>
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult?>> map)
        where T : class
        where TResult : struct
        => value is not null ? await map(value) : null;
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

    /// <summary>
    /// Awaits and returns the result of <paramref name="map"/> when the value
    /// is present; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>. The
    /// mapper may itself return <see langword="null"/>.
    /// </summary>
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult?>> map)
        where T : struct
        where TResult : class
        => value.HasValue ? await map(value.Value) : null;

    /// <summary>
    /// Awaits and returns the result of <paramref name="map"/> when the value
    /// is present; returns <see langword="null"/> without invoking
    /// <paramref name="map"/> when the input is <see langword="null"/>. The
    /// mapper may itself return <see langword="null"/>.
    /// </summary>
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult?>> map)
        where T : class
        where TResult : class
        => value is not null ? await map(value) : null;
}

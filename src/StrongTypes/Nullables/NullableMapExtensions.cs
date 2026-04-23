#nullable enable

using System;
using System.Threading.Tasks;

namespace StrongTypes;

public static class NullableMapToStructExtensions
{
    /// <summary>Applies <paramref name="map"/> when <paramref name="value"/> is present; otherwise returns <c>null</c>.</summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> has a value.</param>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult> map)
        where T : struct
        where TResult : struct
        => value.HasValue ? map(value.Value) : null;

    /// <summary>Applies <paramref name="map"/> when <paramref name="value"/> is present; otherwise returns <c>null</c>. The mapper may itself return <c>null</c>.</summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> has a value; may return <c>null</c>.</param>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult?> map)
        where T : struct
        where TResult : struct
        => value.HasValue ? map(value.Value) : null;

    /// <summary>Applies <paramref name="map"/> when <paramref name="value"/> is non-null; otherwise returns <c>null</c>.</summary>
    /// <typeparam name="T">The source reference type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> is non-null.</param>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult> map)
        where T : class
        where TResult : struct
        => value is not null ? map(value) : null;

    /// <summary>Applies <paramref name="map"/> when <paramref name="value"/> is non-null; otherwise returns <c>null</c>. The mapper may itself return <c>null</c>.</summary>
    /// <typeparam name="T">The source reference type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> is non-null; may return <c>null</c>.</param>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult?> map)
        where T : class
        where TResult : struct
        => value is not null ? map(value) : null;

    /// <summary>Awaits <paramref name="map"/> when <paramref name="value"/> is present; otherwise returns <c>null</c>.</summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> has a value.</param>
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult>> map)
        where T : struct
        where TResult : struct
        => value.HasValue ? await map(value.Value) : null;

    /// <summary>Awaits <paramref name="map"/> when <paramref name="value"/> is present; otherwise returns <c>null</c>. The mapper may itself yield <c>null</c>.</summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> has a value; may yield <c>null</c>.</param>
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult?>> map)
        where T : struct
        where TResult : struct
        => value.HasValue ? await map(value.Value) : null;

    /// <summary>Awaits <paramref name="map"/> when <paramref name="value"/> is non-null; otherwise returns <c>null</c>.</summary>
    /// <typeparam name="T">The source reference type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> is non-null.</param>
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult>> map)
        where T : class
        where TResult : struct
        => value is not null ? await map(value) : null;

    /// <summary>Awaits <paramref name="map"/> when <paramref name="value"/> is non-null; otherwise returns <c>null</c>. The mapper may itself yield <c>null</c>.</summary>
    /// <typeparam name="T">The source reference type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> is non-null; may yield <c>null</c>.</param>
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult?>> map)
        where T : class
        where TResult : struct
        => value is not null ? await map(value) : null;
}

public static class NullableMapToClassExtensions
{
    /// <summary>Applies <paramref name="map"/> when <paramref name="value"/> is present; otherwise returns <c>null</c>. The mapper may itself return <c>null</c>.</summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The result reference type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> has a value; may return <c>null</c>.</param>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult?> map)
        where T : struct
        where TResult : class
        => value.HasValue ? map(value.Value) : null;

    /// <summary>Applies <paramref name="map"/> when <paramref name="value"/> is non-null; otherwise returns <c>null</c>. The mapper may itself return <c>null</c>.</summary>
    /// <typeparam name="T">The source reference type.</typeparam>
    /// <typeparam name="TResult">The result reference type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> is non-null; may return <c>null</c>.</param>
    public static TResult? Map<T, TResult>(this T? value, Func<T, TResult?> map)
        where T : class
        where TResult : class
        => value is not null ? map(value) : null;

    /// <summary>Awaits <paramref name="map"/> when <paramref name="value"/> is present; otherwise returns <c>null</c>. The mapper may itself yield <c>null</c>.</summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The result reference type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> has a value; may yield <c>null</c>.</param>
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult?>> map)
        where T : struct
        where TResult : class
        => value.HasValue ? await map(value.Value) : null;

    /// <summary>Awaits <paramref name="map"/> when <paramref name="value"/> is non-null; otherwise returns <c>null</c>. The mapper may itself yield <c>null</c>.</summary>
    /// <typeparam name="T">The source reference type.</typeparam>
    /// <typeparam name="TResult">The result reference type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> is non-null; may yield <c>null</c>.</param>
    public static async Task<TResult?> MapAsync<T, TResult>(this T? value, Func<T, Task<TResult?>> map)
        where T : class
        where TResult : class
        => value is not null ? await map(value) : null;
}

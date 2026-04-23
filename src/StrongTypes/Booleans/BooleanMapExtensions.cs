#nullable enable

using System;
using System.Threading.Tasks;

namespace StrongTypes;

public static class BooleanMapToStructExtensions
{
    /// <summary>Projects <c>true</c> into a value; otherwise returns <c>null</c>.</summary>
    /// <typeparam name="T">The value type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> is <c>true</c>.</param>
    /// <returns>The result of <paramref name="map"/> when <paramref name="value"/> is <c>true</c>; otherwise <c>null</c>.</returns>
    public static T? MapTrue<T>(this bool value, Func<T> map) where T : struct
        => value ? map() : null;

    /// <summary>Projects <c>true</c> into a value that may itself be <c>null</c>.</summary>
    /// <typeparam name="T">The value type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> is <c>true</c>; may return <c>null</c>.</param>
    /// <returns>The result of <paramref name="map"/> when <paramref name="value"/> is <c>true</c>; otherwise <c>null</c>.</returns>
    public static T? MapTrue<T>(this bool value, Func<T?> map) where T : struct
        => value ? map() : null;

    /// <summary>Projects <c>false</c> into a value; otherwise returns <c>null</c>.</summary>
    /// <typeparam name="T">The value type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> is <c>false</c>.</param>
    /// <returns>The result of <paramref name="map"/> when <paramref name="value"/> is <c>false</c>; otherwise <c>null</c>.</returns>
    public static T? MapFalse<T>(this bool value, Func<T> map) where T : struct
        => value ? null : map();

    /// <summary>Projects <c>false</c> into a value that may itself be <c>null</c>.</summary>
    /// <typeparam name="T">The value type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> is <c>false</c>; may return <c>null</c>.</param>
    /// <returns>The result of <paramref name="map"/> when <paramref name="value"/> is <c>false</c>; otherwise <c>null</c>.</returns>
    public static T? MapFalse<T>(this bool value, Func<T?> map) where T : struct
        => value ? null : map();

    /// <summary>Asynchronously projects <c>true</c> into a value; otherwise returns <c>null</c>.</summary>
    /// <typeparam name="T">The value type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> is <c>true</c>.</param>
    public static async Task<T?> MapTrueAsync<T>(this bool value, Func<Task<T>> map) where T : struct
        => value ? await map() : null;

    /// <summary>Asynchronously projects <c>true</c> into a value that may itself be <c>null</c>.</summary>
    /// <typeparam name="T">The value type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> is <c>true</c>; may yield <c>null</c>.</param>
    public static async Task<T?> MapTrueAsync<T>(this bool value, Func<Task<T?>> map) where T : struct
        => value ? await map() : null;

    /// <summary>Asynchronously projects <c>false</c> into a value; otherwise returns <c>null</c>.</summary>
    /// <typeparam name="T">The value type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> is <c>false</c>.</param>
    public static async Task<T?> MapFalseAsync<T>(this bool value, Func<Task<T>> map) where T : struct
        => value ? null : await map();

    /// <summary>Asynchronously projects <c>false</c> into a value that may itself be <c>null</c>.</summary>
    /// <typeparam name="T">The value type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> is <c>false</c>; may yield <c>null</c>.</param>
    public static async Task<T?> MapFalseAsync<T>(this bool value, Func<Task<T?>> map) where T : struct
        => value ? null : await map();
}

public static class BooleanMapToClassExtensions
{
    /// <summary>Projects <c>true</c> into a reference that may itself be <c>null</c>.</summary>
    /// <typeparam name="T">The reference type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> is <c>true</c>; may return <c>null</c>.</param>
    /// <returns>The result of <paramref name="map"/> when <paramref name="value"/> is <c>true</c>; otherwise <c>null</c>.</returns>
    public static T? MapTrue<T>(this bool value, Func<T?> map) where T : class
        => value ? map() : null;

    /// <summary>Projects <c>false</c> into a reference that may itself be <c>null</c>.</summary>
    /// <typeparam name="T">The reference type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Invoked only when <paramref name="value"/> is <c>false</c>; may return <c>null</c>.</param>
    /// <returns>The result of <paramref name="map"/> when <paramref name="value"/> is <c>false</c>; otherwise <c>null</c>.</returns>
    public static T? MapFalse<T>(this bool value, Func<T?> map) where T : class
        => value ? null : map();

    /// <summary>Asynchronously projects <c>true</c> into a reference that may itself be <c>null</c>.</summary>
    /// <typeparam name="T">The reference type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> is <c>true</c>; may yield <c>null</c>.</param>
    public static async Task<T?> MapTrueAsync<T>(this bool value, Func<Task<T?>> map) where T : class
        => value ? await map() : null;

    /// <summary>Asynchronously projects <c>false</c> into a reference that may itself be <c>null</c>.</summary>
    /// <typeparam name="T">The reference type produced by <paramref name="map"/>.</typeparam>
    /// <param name="value">The flag being mapped.</param>
    /// <param name="map">Awaited only when <paramref name="value"/> is <c>false</c>; may yield <c>null</c>.</param>
    public static async Task<T?> MapFalseAsync<T>(this bool value, Func<Task<T?>> map) where T : class
        => value ? null : await map();
}

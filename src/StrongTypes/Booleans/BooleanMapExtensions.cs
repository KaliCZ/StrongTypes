#nullable enable

using System;
using System.Threading.Tasks;

namespace StrongTypes;

public static class BooleanMapToStructExtensions
{
    /// <summary>
    /// Invokes <paramref name="map"/> and returns its result when
    /// <paramref name="value"/> is <see langword="true"/>; returns
    /// <see langword="null"/> otherwise without invoking <paramref name="map"/>.
    /// </summary>
    public static T? MapTrue<T>(this bool value, Func<T> map) where T : struct
        => value ? map() : null;

    /// <summary>
    /// Invokes <paramref name="map"/> and returns its result when
    /// <paramref name="value"/> is <see langword="false"/>; returns
    /// <see langword="null"/> otherwise without invoking <paramref name="map"/>.
    /// </summary>
    public static T? MapFalse<T>(this bool value, Func<T> map) where T : struct
        => value ? null : map();

    /// <summary>
    /// Awaits and returns the result of <paramref name="map"/> when
    /// <paramref name="value"/> is <see langword="true"/>; returns
    /// <see langword="null"/> otherwise without invoking <paramref name="map"/>.
    /// </summary>
    public static async Task<T?> MapTrueAsync<T>(this bool value, Func<Task<T>> map) where T : struct
        => value ? await map() : null;

    /// <summary>
    /// Awaits and returns the result of <paramref name="map"/> when
    /// <paramref name="value"/> is <see langword="false"/>; returns
    /// <see langword="null"/> otherwise without invoking <paramref name="map"/>.
    /// </summary>
    public static async Task<T?> MapFalseAsync<T>(this bool value, Func<Task<T>> map) where T : struct
        => value ? null : await map();
}

public static class BooleanMapToClassExtensions
{
    /// <summary>
    /// Invokes <paramref name="map"/> and returns its result when
    /// <paramref name="value"/> is <see langword="true"/>; returns
    /// <see langword="null"/> otherwise without invoking <paramref name="map"/>.
    /// The mapper may itself return <see langword="null"/>.
    /// </summary>
    public static T? MapTrue<T>(this bool value, Func<T?> map) where T : class
        => value ? map() : null;

    /// <summary>
    /// Invokes <paramref name="map"/> and returns its result when
    /// <paramref name="value"/> is <see langword="false"/>; returns
    /// <see langword="null"/> otherwise without invoking <paramref name="map"/>.
    /// The mapper may itself return <see langword="null"/>.
    /// </summary>
    public static T? MapFalse<T>(this bool value, Func<T?> map) where T : class
        => value ? null : map();

    /// <summary>
    /// Awaits and returns the result of <paramref name="map"/> when
    /// <paramref name="value"/> is <see langword="true"/>; returns
    /// <see langword="null"/> otherwise without invoking <paramref name="map"/>.
    /// The mapper may itself return <see langword="null"/>.
    /// </summary>
    public static async Task<T?> MapTrueAsync<T>(this bool value, Func<Task<T?>> map) where T : class
        => value ? await map() : null;

    /// <summary>
    /// Awaits and returns the result of <paramref name="map"/> when
    /// <paramref name="value"/> is <see langword="false"/>; returns
    /// <see langword="null"/> otherwise without invoking <paramref name="map"/>.
    /// The mapper may itself return <see langword="null"/>.
    /// </summary>
    public static async Task<T?> MapFalseAsync<T>(this bool value, Func<Task<T?>> map) where T : class
        => value ? null : await map();
}

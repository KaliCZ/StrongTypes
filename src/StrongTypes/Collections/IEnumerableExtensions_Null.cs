#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>Returns <paramref name="source"/>, or an empty sequence when it is <c>null</c>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence that may be <c>null</c>.</param>
    public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T>? source)
        => source ?? Enumerable.Empty<T>();

    /// <summary>Returns <paramref name="source"/>, or an empty array when it is <c>null</c>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The array that may be <c>null</c>.</param>
    public static T[] OrEmptyIfNull<T>(this T[]? source)
        => source ?? Array.Empty<T>();

    /// <summary>Returns <paramref name="source"/>, or a new empty list when it is <c>null</c>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The list that may be <c>null</c>.</param>
    public static List<T> OrEmptyIfNull<T>(this List<T>? source)
        => source ?? new List<T>();

    /// <summary>Returns <paramref name="source"/>, or an empty list when it is <c>null</c>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The list that may be <c>null</c>.</param>
    public static IReadOnlyList<T> OrEmptyIfNull<T>(this IReadOnlyList<T>? source)
        => source ?? Array.Empty<T>();

    /// <summary>Returns <paramref name="source"/>, or an empty collection when it is <c>null</c>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The collection that may be <c>null</c>.</param>
    public static ICollection<T> OrEmptyIfNull<T>(this ICollection<T>? source)
        => source ?? Array.Empty<T>();
}

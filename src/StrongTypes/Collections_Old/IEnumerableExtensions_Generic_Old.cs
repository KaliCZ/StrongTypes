using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Uses ToArray to generate an IReadOnlyList.
    /// </summary>
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> e)
    {
        return e.ToArray();
    }

    /// <summary>
    /// Returns the IEnumerable in case it is a ReadOnlyList or creates a new ReadOnlyList from it.
    /// </summary>
    [DebuggerStepThrough]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> source)
    {
        return source as IReadOnlyList<T> ?? source.ToArray();
    }

    /// <summary>
    /// Returns the array in case it is a ReadOnlyList or creates a new ReadOnlyList from it.
    /// </summary>
    [DebuggerStepThrough]
    [Pure]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this T[] source)
    {
        return source;
    }

    /// <summary>
    /// Returns the list in case it is a ReadOnlyList or creates a new ReadOnlyList from it.
    /// </summary>
    [DebuggerStepThrough]
    [Pure]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this List<T> source)
    {
        return source;
    }

    /// <summary>
    /// Returns the list in case it is a ReadOnlyList or creates a new ReadOnlyList from it.
    /// </summary>
    [DebuggerStepThrough]
    [Pure]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this INonEmptyEnumerable<T> source)
    {
        return source;
    }

    [Obsolete("This already is of type ReadOnlyList.", error: true)]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this IReadOnlyList<T> source)
    {
        return source;
    }

    /// <summary>
    /// Returns the IEnumerable in case it is a List or creates a new List from it.
    /// </summary>
    [DebuggerStepThrough]
    public static List<T> AsList<T>(this IEnumerable<T> source)
    {
        return source as List<T> ?? source.ToList();
    }

    /// <summary>
    /// Returns the IEnumerable in case it is a Array or creates a new Array from it.
    /// </summary>
    [DebuggerStepThrough]
    public static T[] AsArray<T>(this IEnumerable<T> source)
    {
        return source as T[] ?? source.ToArray();
    }

    public static IEnumerable<T> Except<T>(this IEnumerable<T> e, params T[] excludedItems)
    {
        return Enumerable.Except(e, excludedItems);
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>Materializes <paramref name="source"/> as a read-only list (by copying).</summary>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
    [Pure]
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source) => source.ToArray();

    /// <summary>Returns <paramref name="source"/> as a read-only list, copying only when it is not already one.</summary>
    [DebuggerStepThrough, Pure]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> source) => source as IReadOnlyList<T> ?? source.ToArray();

    [DebuggerStepThrough, Pure]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this T[] source) => source;

    [DebuggerStepThrough, Pure]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this List<T> source) => source;

    [DebuggerStepThrough, Pure]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this INonEmptyEnumerable<T> source) => source;

    [Obsolete("This already is of type ReadOnlyList.", error: true)]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this IReadOnlyList<T> source) => source;

    /// <summary>Returns <paramref name="source"/> as a <see cref="List{T}"/>, copying only when it is not already one.</summary>
    [DebuggerStepThrough, Pure]
    public static List<T> AsList<T>(this IEnumerable<T> source) => source as List<T> ?? source.ToList();

    /// <summary>Returns <paramref name="source"/> as an array, copying only when it is not already one.</summary>
    [DebuggerStepThrough, Pure]
    public static T[] AsArray<T>(this IEnumerable<T> source) => source as T[] ?? source.ToArray();
}

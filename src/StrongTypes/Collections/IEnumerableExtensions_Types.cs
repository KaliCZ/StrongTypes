#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source) => source.ToArray();

    [DebuggerStepThrough]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> source)
        => source as IReadOnlyList<T> ?? source.ToArray();

    [DebuggerStepThrough, Pure]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this T[] source) => source;

    [DebuggerStepThrough, Pure]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this List<T> source) => source;

    [DebuggerStepThrough, Pure]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this INonEmptyEnumerable<T> source) => source;

    [Obsolete("This already is of type ReadOnlyList.", error: true)]
    public static IReadOnlyList<T> AsReadOnlyList<T>(this IReadOnlyList<T> source) => source;

    [DebuggerStepThrough]
    public static List<T> AsList<T>(this IEnumerable<T> source)
        => source as List<T> ?? source.ToList();

    [DebuggerStepThrough]
    public static T[] AsArray<T>(this IEnumerable<T> source)
        => source as T[] ?? source.ToArray();
}

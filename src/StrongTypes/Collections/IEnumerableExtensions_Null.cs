using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    public static IEnumerable<T> OrEmptyIfNull<T>(this IEnumerable<T>? source)
        => source ?? Enumerable.Empty<T>();

    public static T[] OrEmptyIfNull<T>(this T[]? source)
        => source ?? Array.Empty<T>();

    public static List<T> OrEmptyIfNull<T>(this List<T>? source)
        => source ?? new List<T>();

    public static IReadOnlyList<T> OrEmptyIfNull<T>(this IReadOnlyList<T>? source)
        => source ?? Array.Empty<T>();

    public static ICollection<T> OrEmptyIfNull<T>(this ICollection<T>? source)
        => source ?? Array.Empty<T>();
}

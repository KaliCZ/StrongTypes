using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static class ReadOnlyList
{
    [Pure]
    public static IReadOnlyList<T> Create<T>(params T[] values)
    {
        return CreateFlat(values);
    }

    public static IReadOnlyList<T> Create<T>(T head, IEnumerable<T> tail)
    {
        var list = new List<T> { head };
        list.AddRange(tail);
        return list;
    }

    public static IReadOnlyList<T> CreateFlat<T>(params IEnumerable<T>[] values)
    {
        return values.Flatten().ToArray();
    }

    [Pure]
    public static IReadOnlyList<T> CreateFlat<T>(params Maybe<T>[] values) where T : notnull
    {
        return values.Values().ToArray();
    }

    public static IReadOnlyList<T> CreateFlat<T>(params IEnumerable<Maybe<T>>[] values) where T : notnull
    {
        return values.Flatten().Values().ToArray();
    }

    public static IReadOnlyList<T> CreateFlat<T>(params Maybe<IEnumerable<T>>[] values)
    {
        return values.Values().Flatten().ToArray();
    }

    [Pure]
    public static IReadOnlyList<T> CreateFlat<T>(params Maybe<IReadOnlyList<T>>[] values)
    {
        return values.Values().Flatten().ToArray();
    }

    [Pure]
    public static IReadOnlyList<T> CreateFlat<T>(params Maybe<List<T>>[] values)
    {
        return values.Values().Flatten().ToArray();
    }

    [Pure]
    public static IReadOnlyList<T> CreateFlat<T>(params Maybe<T[]>[] values)
    {
        return values.Values().Flatten().ToArray();
    }

    [Pure]
    public static IReadOnlyList<T> CreateFlat<T>(params Maybe<INonEmptyEnumerable<T>>[] values)
    {
        return values.Values().Flatten().ToArray();
    }

    [Pure]
    public static IReadOnlyList<T> Empty<T>()
    {
        return ReadOnlyList<T>.Empty;
    }
}

public class ReadOnlyList<T>
{
    public static readonly ReadOnlyCollection<T> EmptyReadOnlyCollection = new List<T>().AsReadOnly();
    public static readonly IReadOnlyList<T> Empty = EmptyReadOnlyCollection;
}

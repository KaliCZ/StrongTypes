using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static class ReadOnlyList
{
    [Pure]
    public static IReadOnlyList<T> Create<T>(params T[] values)
    {
        return values;
    }

    public static IReadOnlyList<T> CreateFlat<T>(params IEnumerable<T>[] values)
    {
        return values.Flatten().ToArray();
    }
}

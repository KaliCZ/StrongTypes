#nullable enable

using System.Collections.Generic;

namespace StrongTypes;

public static class IReadOnlyListExtensions
{
    /// <summary>
    /// Returns the element of <paramref name="list"/> at <paramref name="index"/>.
    /// The <see cref="NonNegative{T}"/> parameter rules out negative indices at
    /// the type level; out-of-range access still throws as per the list's
    /// indexer.
    /// </summary>
    public static T ElementAt<T>(this IReadOnlyList<T> list, NonNegative<int> index)
    {
        return list[index.Value];
    }
}

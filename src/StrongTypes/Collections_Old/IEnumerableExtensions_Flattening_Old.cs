using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Returns all the items inside all the collections combined into 1 IEnumerable.
    /// </summary>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> e)
    {
        return e.SelectMany(i => i);
    }

    // The INonEmptyEnumerable<INonEmptyEnumerable<T>> overload has been migrated to
    // StrongTypes.NonEmptyEnumerableExtensions.Flatten.
}
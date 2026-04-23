#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>Concatenates the inner sequences of <paramref name="source"/>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The outer sequence.</param>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> source)
        => source.SelectMany(i => i);
}

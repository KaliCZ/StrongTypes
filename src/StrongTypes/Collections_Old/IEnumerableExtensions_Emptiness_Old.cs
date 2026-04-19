#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Returns an <see cref="INonEmptyEnumerable{T}"/> when the source has at least one element,
    /// otherwise <c>null</c>.
    /// </summary>
    [DebuggerStepThrough]
    public static INonEmptyEnumerable<T>? AsNonEmpty<T>(this IEnumerable<T>? source)
    {
        return source switch
        {
            null => null,
            INonEmptyEnumerable<T> list => list,
            _ => NonEmptyEnumerable.Create(source)
        };
    }

    /// <summary>
    /// The source is already non-empty; callers shouldn't need to re-assert that.
    /// </summary>
    [Obsolete("This is already a NonEmptyEnumerable.", error: true)]
    public static INonEmptyEnumerable<T> AsNonEmpty<T>(this INonEmptyEnumerable<T> source)
    {
        return source;
    }

    /// <summary>
    /// Returns true if the collection contains at least one element.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> parameter is null.</exception>
    public static bool NonEmpty<T>(this IEnumerable<T> source)
    {
        return source.Any();
    }

    /// <summary>
    /// Returns true if the collection contains no elements.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> parameter is null.</exception>
    public static bool IsEmpty<T>(this IEnumerable<T> source)
    {
        return !source.Any();
    }

    /// <summary>
    /// Returns true if the collection contains at least one element.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> parameter is null.</exception>
    [Pure]
    public static bool NonEmpty<T>(this IReadOnlyCollection<T> source)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        return source.Count > 0;
    }

    /// <summary>
    /// Returns true if the collection contains no elements.
    /// </summary>
    /// <exception cref="System.ArgumentNullException">The <paramref name="source"/> parameter is null.</exception>
    [Pure]
    public static bool IsEmpty<T>(this IReadOnlyCollection<T> source)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        return source.Count == 0;
    }

    [Obsolete("This is a NonEmptyEnumerable. It's not empty.", error: true)]
    public static bool NonEmpty<T>(this INonEmptyEnumerable<T> source)
    {
        return true;
    }

    [Obsolete("This is a NonEmptyEnumerable. It's not empty.", error: true)]
    public static bool IsEmpty<T>(this INonEmptyEnumerable<T> source)
    {
        return false;
    }
}
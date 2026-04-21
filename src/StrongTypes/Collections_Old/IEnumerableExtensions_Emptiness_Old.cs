using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    // The Option<T>-returning AsNonEmpty overloads that used to live here have been
    // superseded by NonEmptyEnumerableExtensions.AsNonEmpty / ToNonEmpty in the new
    // Collections/ slice. The remaining NonEmpty / IsEmpty checks are unrelated to
    // NonEmptyEnumerable<T> and stay here until their own migration pass.

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
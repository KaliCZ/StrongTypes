using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    [Pure]
    public static T Last<T>(this IReadOnlyList<T> list)
    {
        return list.Count == 0
            ? throw new ArgumentException("Source is empty.")
            : list[list.Count - 1];
    }

    [Pure]
    public static T ElementAt<T>(this IReadOnlyList<T> list, int index)
    {
        return list[index];
    }

    [Pure]
    public static int IndexOf<T>(this IReadOnlyList<T> list, T item)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (Equals(list[i], item))
            {
                return i;
            }
        }
        return -1;
    }
}
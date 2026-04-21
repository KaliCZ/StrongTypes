using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
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
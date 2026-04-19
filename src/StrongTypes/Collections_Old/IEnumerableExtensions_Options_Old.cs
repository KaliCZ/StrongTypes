using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Returns values of the nonempty options. Kept because <c>Try_Old</c> still
    /// depends on it; the rest of the Option→IEnumerable surface was dropped.
    /// </summary>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<Option<T>> source)
    {
        return source.Where(o => o.NonEmpty).Select(o => o.Value);
    }
}

using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Returns values of the nonempty options. Kept because <c>Try_Old</c> still
    /// depends on it; will disappear along with Option when Try migrates to Maybe.
    /// </summary>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<Option<T>> source)
    {
        return source.Where(o => o.NonEmpty).Select(o => o.Value);
    }
}

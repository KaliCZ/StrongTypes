using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    public static NonPositiveInt Sum(this IEnumerable<NonPositiveInt> values)
    {
        return values.Aggregate(NonPositiveInt.Zero, (a, b) => a + b);
    }

    public static NonPositiveDecimal Sum(this IEnumerable<NonPositiveDecimal> values)
    {
        return values.Aggregate(NonPositiveDecimal.Zero, (a, b) => a + b);
    }
}
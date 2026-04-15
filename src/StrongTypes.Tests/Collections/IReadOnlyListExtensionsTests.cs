#nullable enable

using System;
using System.Collections.Generic;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class IReadOnlyListExtensionsTests
{
    [Property]
    public void ElementAt_ReturnsIndexerResult_WhenInRange(int[] values, int rawIndex)
    {
        if (values.Length == 0)
        {
            return;
        }

        // Map rawIndex into a valid non-negative in-range index.
        var positiveIndex = (rawIndex & int.MaxValue) % values.Length;
        var index = NonNegative<int>.Create(positiveIndex);

        IReadOnlyList<int> list = values;
        Assert.Equal(values[positiveIndex], list.ElementAt(index));
    }

    [Fact]
    public void ElementAt_ZeroDefault_ReturnsFirstElement()
    {
        IReadOnlyList<int> list = new[] { 10, 20, 30 };
        Assert.Equal(10, list.ElementAt(default));
    }

    [Fact]
    public void ElementAt_OutOfRange_Throws()
    {
        IReadOnlyList<int> list = new[] { 1, 2, 3 };
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => list.ElementAt(NonNegative<int>.Create(5)));
    }
}

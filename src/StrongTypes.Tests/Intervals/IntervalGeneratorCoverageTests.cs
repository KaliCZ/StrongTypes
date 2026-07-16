using FsCheck.Fluent;
using Xunit;

namespace StrongTypes.Tests;

// A generator branch that stops emitting would silently shrink every consuming property test's coverage.
public class IntervalGeneratorCoverageTests
{
    [Fact]
    public void IntervalInt_CoversAllFourNullabilityBranches()
    {
        var samples = Generators.IntervalInt.Generator.Sample(500);
        Assert.Contains(samples, i => i.Start is null && i.End is null);
        Assert.Contains(samples, i => i.Start is null && i.End is not null);
        Assert.Contains(samples, i => i.Start is not null && i.End is null);
        Assert.Contains(samples, i => i.Start is not null && i.End is not null);
    }

    [Fact]
    public void IntervalFromInt_CoversPresentAndAbsentEnd()
    {
        var samples = Generators.IntervalFromInt.Generator.Sample(500);
        Assert.Contains(samples, i => i.End is null);
        Assert.Contains(samples, i => i.End is not null);
    }

    [Fact]
    public void IntervalUntilInt_CoversPresentAndAbsentStart()
    {
        var samples = Generators.IntervalUntilInt.Generator.Sample(500);
        Assert.Contains(samples, i => i.Start is null);
        Assert.Contains(samples, i => i.Start is not null);
    }
}

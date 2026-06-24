using FsCheck.Fluent;
using Xunit;

namespace StrongTypes.Tests;

// Guards the weighted Gen.Frequency branches in the interval arbitraries: a
// regression that stopped emitting (say) the unbounded case would silently
// shrink coverage of every property test that consumes these generators.
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

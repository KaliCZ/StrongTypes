using FsCheck;
using FsCheck.Xunit;
using StrongTypes.Tests.Generative;
using Xunit;

namespace StrongTypes.Tests.Options;

[Properties(Arbitrary = new[] { typeof(OptionGenerators) })]
public class IsEmptyTests
{
    [Fact]
    public void IsEmpty()
    {
        Assert.False(42.ToOption().IsEmpty);
        Assert.False((42 as int?).ToOption().IsEmpty);
        Assert.True((null as int?).ToOption().IsEmpty);

        Assert.False(new object().ToOption().IsEmpty);
        Assert.True((null as object).ToOption().IsEmpty);

        Assert.False("foo".ToOption().IsEmpty);
        Assert.True((null as string).ToOption().IsEmpty);
    }

    [Property]
    internal void IsEmpty_int(int i)
    {
        AssertIsEmpty(i);
    }

    [Property]
    internal void IsEmpty_decimal(decimal option)
    {
        AssertIsEmpty(option);
    }

    [Property]
    internal void IsEmpty_double(double option)
    {
        AssertIsEmpty(option);
    }

    [Property]
    internal void IsEmpty_bool(bool option)
    {
        AssertIsEmpty(option);
    }

    [Property]
    internal void IsEmpty_ReferenceType(ReferenceType option)
    {
        AssertIsEmpty(option);
    }

    private void AssertIsEmpty<T>(T value)
    {
        Assert.False(Option.Valued(value).IsEmpty);
        Assert.True(Option.Empty<T>().IsEmpty);
    }
}
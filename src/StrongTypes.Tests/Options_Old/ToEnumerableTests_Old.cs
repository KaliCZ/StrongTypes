using FsCheck;
using FsCheck.Xunit;
using StrongTypes.Tests.Generative;
using Xunit;

namespace StrongTypes.Tests.Options;

[Properties(Arbitrary = new[] { typeof(OptionGenerators) })]
public class ToEnumerableTests
{
    [Fact]
    public void ToEnumerable()
    {
        Assert.NotEmpty(42.ToOption().AsReadOnlyList());
        Assert.NotEmpty((42 as int?).ToOption().AsReadOnlyList());
        Assert.Empty((null as int?).ToOption().AsReadOnlyList());

        Assert.NotEmpty(new object().ToOption().AsReadOnlyList());
        Assert.Empty((null as object).ToOption().AsReadOnlyList());

        Assert.NotEmpty("foo".ToOption().AsReadOnlyList());
        Assert.Empty((null as string).ToOption().AsReadOnlyList());
    }

    [Property]
    internal void ToEnumerable_int(int i)
    {
        AssertToEnumerable(i);
    }

    [Property]
    internal void ToEnumerable_decimal(decimal option)
    {
        AssertToEnumerable(option);
    }

    [Property]
    internal void ToEnumerable_double(double option)
    {
        AssertToEnumerable(option);
    }

    [Property]
    internal void ToEnumerable_bool(bool option)
    {
        AssertToEnumerable(option);
    }

    [Property]
    internal void ToEnumerable_ReferenceType(ReferenceType option)
    {
        AssertToEnumerable(option);
    }

    private void AssertToEnumerable<T>(T value)
    {
        Assert.NotEmpty(Option.Valued(value).AsReadOnlyList());
        Assert.Empty(Option.Empty<T>().AsReadOnlyList());
    }
}
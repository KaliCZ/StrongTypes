using FsCheck;
using FsCheck.Xunit;
using StrongTypes.Tests.Generative;
using Xunit;

namespace StrongTypes.Tests.Options;

[Properties(Arbitrary = new[] { typeof(OptionGenerators) })]
public class GetOrNullTests
{
    [Fact]
    public void GetOrNull()
    {
        Assert.Equal(new ReferenceType(14), new ReferenceType(14).ToOption().GetOrNull());
        Assert.Null(Option.Valued<ReferenceType>(null).GetOrNull());
        Assert.Null(Option.Empty<ReferenceType>().GetOrNull());
    }

    [Property]
    internal void GetOrNull_bool(Option<ReferenceType> option)
    {
        AssertGetOrNull(option);
    }

    private void AssertGetOrNull<T>(Option<T> option)
        where T: class
    {
        var result = option.GetOrNull();
        if (option.NonEmpty)
        {
            Assert.NotNull(result);
            Assert.Equal(option.GetOrDefault(), result);
        }
        else
        {
            Assert.Null(result);
        }
    }
}
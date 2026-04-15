using FsCheck;
using FsCheck.Xunit;
using StrongTypes.Tests.Generative;
using Xunit;

namespace StrongTypes.Tests.Options;

public class GetOrFalseTests
{
    public GetOrFalseTests()
    {
        Arb.Register<OptionGenerators>();
    }

    [Fact]
    public void GetOrFalse()
    {
        Assert.True(true.ToOption().GetOrFalse());
        Assert.False(false.ToOption().GetOrFalse());
        Assert.False(Option.Empty<bool>().GetOrFalse());
    }

    [Property]
    internal void GetOrFalse_bool(Option<bool> option)
    {
        Assert.Equal(option.GetOrDefault(), option.GetOrFalse());
    }
}
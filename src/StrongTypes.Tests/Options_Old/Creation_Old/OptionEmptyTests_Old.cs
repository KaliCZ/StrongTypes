using FsCheck;
using FsCheck.Xunit;
using StrongTypes.Tests.Generative;
using Xunit;

namespace StrongTypes.Tests.Options;

[Properties(Arbitrary = new[] { typeof(OptionGenerators) })]
public class OptionEmptyTests
{
    [Fact]
    public void Empty()
    {
        OptionAssert.IsEmpty(Option.Empty<int>());
        OptionAssert.IsEmpty(Option.Empty<int?>());
        OptionAssert.IsEmpty(Option.Empty<ReferenceType>());
        OptionAssert.IsEmpty(Option.Empty<Unit>());
    }
}
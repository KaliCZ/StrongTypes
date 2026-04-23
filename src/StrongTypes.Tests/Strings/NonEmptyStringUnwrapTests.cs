using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonEmptyStringUnwrapTests
{
    [Property]
    public void Unwrap_ReturnsUnderlyingValue(NonEmptyString? s)
    {
        if (s is null)
        {
            return;
        }
        Assert.Same(s.Value, s.Unwrap());
    }
}

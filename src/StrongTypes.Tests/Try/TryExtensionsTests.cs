#nullable enable

using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(StringGenerators) })]
public class TryExtensionsTests
{
    // Two-value enum where Missing is deliberately non-default so we can
    // assert that ToTry actually propagates the factory's return value
    // rather than a default-initialized placeholder.
    private enum ParseError
    {
        Unused = 0,
        Missing = 1
    }

    [Property]
    public void ToTry_ReferenceType(NonEmptyString? value)
    {
        var calls = 0;
        var result = value.ToTry(() => { calls++; return ParseError.Missing; });

        if (value is null)
        {
            Assert.Equal(1, calls);
            Assert.True(result.IsError);
            var error = result.Error.Get();
            Assert.Equal(ParseError.Missing, error);
            Assert.NotEqual(default(ParseError), error);
        }
        else
        {
            Assert.Equal(0, calls);
            Assert.True(result.IsSuccess);
            Assert.Equal(value, result.Success.Get());
        }
    }

    [Property]
    public void ToTry_ValueType(int? value)
    {
        var calls = 0;
        var result = value.ToTry(() => { calls++; return ParseError.Missing; });

        if (value is null)
        {
            Assert.Equal(1, calls);
            Assert.True(result.IsError);
            var error = result.Error.Get();
            Assert.Equal(ParseError.Missing, error);
            Assert.NotEqual(default(ParseError), error);
        }
        else
        {
            Assert.Equal(0, calls);
            Assert.True(result.IsSuccess);
            Assert.Equal(value.Value, result.Success.Get());
        }
    }

    [Fact]
    public void NullableNonEmptyString_GeneratorProducesBothBranches()
    {
        // Sanity check that the property tests above are actually hitting
        // both branches of the ToTry logic, not e.g. always exercising the
        // null path because the non-null generator silently fails.
        // 200 samples at 10% null rate: P(zero nulls) ≈ 0.9^200 ≈ 7e-10.
        var samples = StringGenerators.NullableNonEmptyString.Generator.Sample(200);

        Assert.Contains(samples, s => s is null);
        Assert.Contains(samples, s => s is not null);
    }
}

#nullable enable

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(TryExtensionsTests.Generators) })]
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

    public static class Generators
    {
        public static Arbitrary<NonEmptyString?> NullableNonEmptyString { get; } =
            Arb.From(Gen.OneOf(
                Gen.Constant<NonEmptyString?>(null),
                ArbMap.Default.ArbFor<string>().Generator
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => (NonEmptyString?)NonEmptyString.Create(s))));
    }
}

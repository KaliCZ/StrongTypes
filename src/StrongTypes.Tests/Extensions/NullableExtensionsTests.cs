#nullable enable

using Xunit;

namespace StrongTypes.Tests.Extensions;

public class NullableExtensionsTests
{
    private enum ParseError
    {
        Missing
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("hello world")]
    public void ToTry_ReferenceType_NonNull_IsSuccess(string input)
    {
        NonEmptyString? value = NonEmptyString.TryCreate(input);

        Try<NonEmptyString, ParseError> result = value.ToTry(() => ParseError.Missing);

        Assert.True(result.IsSuccess);
        Assert.Equal(input, result.Success.Get()!.Value);
    }

    [Fact]
    public void ToTry_ReferenceType_Null_IsErrorWithProducedValue()
    {
        NonEmptyString? value = null;

        Try<NonEmptyString, ParseError> result = value.ToTry(() => ParseError.Missing);

        Assert.True(result.IsError);
        Assert.Equal(ParseError.Missing, result.Error.Get());
    }

    [Theory]
    [InlineData(42)]
    [InlineData(0)]        // default(int) — dispatch must go on HasValue, not value equality with default
    [InlineData(-7)]
    public void ToTry_ValueType_HasValue_IsSuccess(int input)
    {
        int? value = input;

        Try<int, ParseError> result = value.ToTry(() => ParseError.Missing);

        Assert.True(result.IsSuccess);
        Assert.Equal(input, result.Success.Get());
    }

    [Fact]
    public void ToTry_ValueType_Null_IsErrorWithProducedValue()
    {
        int? value = null;

        Try<int, ParseError> result = value.ToTry(() => ParseError.Missing);

        Assert.True(result.IsError);
        Assert.Equal(ParseError.Missing, result.Error.Get());
    }

    [Fact]
    public void ToTry_ReferenceType_ErrorFactory_InvokedOnlyOnNull()
    {
        var calls = 0;
        NonEmptyString? populated = NonEmptyString.TryCreate("abc");
        NonEmptyString? empty = null;

        _ = populated.ToTry(() => { calls++; return ParseError.Missing; });
        Assert.Equal(0, calls);

        _ = empty.ToTry(() => { calls++; return ParseError.Missing; });
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ToTry_ValueType_ErrorFactory_InvokedOnlyOnNull()
    {
        var calls = 0;
        int? populated = 42;
        int? empty = null;

        _ = populated.ToTry(() => { calls++; return ParseError.Missing; });
        Assert.Equal(0, calls);

        _ = empty.ToTry(() => { calls++; return ParseError.Missing; });
        Assert.Equal(1, calls);
    }
}

#nullable enable

using Xunit;

namespace StrongTypes.Tests.Extensions;

public class NullableExtensionsTests
{
    private enum ParseError
    {
        Missing
    }

    [Fact]
    public void ToTry_ReferenceType_NonNull_IsSuccess()
    {
        NonEmptyString? value = NonEmptyString.TryCreate("abc");

        Try<NonEmptyString, ParseError> result = value.ToTry(() => ParseError.Missing);

        Assert.True(result.IsSuccess);
        Assert.Equal("abc", result.Success.Get()!.Value);
    }

    [Fact]
    public void ToTry_ReferenceType_Null_IsErrorWithProducedValue()
    {
        NonEmptyString? value = NonEmptyString.TryCreate("");

        Try<NonEmptyString, ParseError> result = value.ToTry(() => ParseError.Missing);

        Assert.True(result.IsError);
        Assert.Equal(ParseError.Missing, result.Error.Get());
    }

    [Fact]
    public void ToTry_ReferenceType_Null_InvokesErrorFactoryOnlyOnNull()
    {
        var calls = 0;
        NonEmptyString? populated = NonEmptyString.TryCreate("abc");
        NonEmptyString? empty = NonEmptyString.TryCreate("");

        _ = populated.ToTry(() => { calls++; return ParseError.Missing; });
        Assert.Equal(0, calls);

        _ = empty.ToTry(() => { calls++; return ParseError.Missing; });
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ToTry_ValueType_HasValue_IsSuccess()
    {
        int? value = 42;

        Try<int, ParseError> result = value.ToTry(() => ParseError.Missing);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Success.Get());
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
    public void ToTry_ValueType_HasValue_ZeroIsSuccess()
    {
        // Ensures we dispatch on HasValue, not on the underlying value being "default".
        int? value = 0;

        Try<int, ParseError> result = value.ToTry(() => ParseError.Missing);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Success.Get());
    }
}

#nullable enable

using System.Text.Json;
using Xunit;

namespace StrongTypes.Tests;

/// <summary>
/// Round-trip coverage for DTOs whose properties are <see cref="Maybe{T}"/>
/// and <see cref="Maybe{T}"/>? — the realistic request-body shape. Each case
/// asserts both directions against a single canonical JSON string: reading
/// the string produces the expected DTO, and serializing the DTO produces
/// the same string back.
/// </summary>
public class MaybeDtoJsonTests
{
    private sealed record IntDto(Maybe<int> Value, Maybe<int>? NullableValue);
    private sealed record StringDto(Maybe<string> Value, Maybe<string>? NullableValue);

    private static void AssertRoundTrip<T>(T value, string json)
    {
        Assert.Equal(json, JsonSerializer.Serialize(value));
        Assert.Equal(value, JsonSerializer.Deserialize<T>(json));
    }

    // ── IntDto: Maybe<int> required, Maybe<int>? optional ───────────────

    [Fact]
    public void IntDto_Some_NullableNull() => AssertRoundTrip(
        new IntDto(Maybe.Some(7), null),
        """{"Value":{"Value":7},"NullableValue":null}""");

    [Fact]
    public void IntDto_Some_NullableSome() => AssertRoundTrip(
        new IntDto(Maybe.Some(7), Maybe.Some(3)),
        """{"Value":{"Value":7},"NullableValue":{"Value":3}}""");

    [Fact]
    public void IntDto_Some_NullableNone() => AssertRoundTrip(
        new IntDto(Maybe.Some(7), Maybe<int>.None),
        """{"Value":{"Value":7},"NullableValue":{"Value":null}}""");

    [Fact]
    public void IntDto_None_NullableNull() => AssertRoundTrip(
        new IntDto(Maybe<int>.None, null),
        """{"Value":{"Value":null},"NullableValue":null}""");

    [Fact]
    public void IntDto_None_NullableSome() => AssertRoundTrip(
        new IntDto(Maybe<int>.None, Maybe.Some(3)),
        """{"Value":{"Value":null},"NullableValue":{"Value":3}}""");

    [Fact]
    public void IntDto_None_NullableNone() => AssertRoundTrip(
        new IntDto(Maybe<int>.None, Maybe<int>.None),
        """{"Value":{"Value":null},"NullableValue":{"Value":null}}""");

    // ── StringDto: reference-type payload, same state matrix ────────────

    [Fact]
    public void StringDto_Some_NullableNull() => AssertRoundTrip(
        new StringDto(Maybe.Some("hi"), null),
        """{"Value":{"Value":"hi"},"NullableValue":null}""");

    [Fact]
    public void StringDto_Some_NullableSome() => AssertRoundTrip(
        new StringDto(Maybe.Some("hi"), Maybe.Some("there")),
        """{"Value":{"Value":"hi"},"NullableValue":{"Value":"there"}}""");

    [Fact]
    public void StringDto_Some_NullableNone() => AssertRoundTrip(
        new StringDto(Maybe.Some("hi"), Maybe<string>.None),
        """{"Value":{"Value":"hi"},"NullableValue":{"Value":null}}""");

    [Fact]
    public void StringDto_None_NullableNull() => AssertRoundTrip(
        new StringDto(Maybe<string>.None, null),
        """{"Value":{"Value":null},"NullableValue":null}""");

    [Fact]
    public void StringDto_None_NullableSome() => AssertRoundTrip(
        new StringDto(Maybe<string>.None, Maybe.Some("there")),
        """{"Value":{"Value":null},"NullableValue":{"Value":"there"}}""");

    [Fact]
    public void StringDto_None_NullableNone() => AssertRoundTrip(
        new StringDto(Maybe<string>.None, Maybe<string>.None),
        """{"Value":{"Value":null},"NullableValue":{"Value":null}}""");
}

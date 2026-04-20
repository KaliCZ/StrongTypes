#nullable enable

using System.Linq;
using System.Text.Json;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonEmptyEnumerableJsonConverterTests
{
    // ── Read ────────────────────────────────────────────────────────────

    [Fact]
    public void Read_Array_DeserializesToNonEmpty()
    {
        var list = JsonSerializer.Deserialize<NonEmptyEnumerable<int>>("[1,2,3]");
        Assert.NotNull(list);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void Read_Null_ReturnsNull()
    {
        var list = JsonSerializer.Deserialize<NonEmptyEnumerable<int>>("null");
        Assert.Null(list);
    }

    [Fact]
    public void Read_EmptyArray_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NonEmptyEnumerable<int>>("[]"));
    }

    [Fact]
    public void Read_NonArray_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NonEmptyEnumerable<int>>("42"));

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NonEmptyEnumerable<int>>("""{"x":1}"""));
    }

    [Fact]
    public void Read_NullElement_OfNullableReferenceType_IsKept()
    {
        // The type's invariant is Count >= 1, not "no null elements". For reference T,
        // `NonEmptyEnumerable<string>` and `NonEmptyEnumerable<string?>` erase to the same
        // runtime type, so the converter can't enforce non-null without also breaking
        // the legitimate nullable case — and the factories already allow nulls through.
        var list = JsonSerializer.Deserialize<NonEmptyEnumerable<string?>>("""["a",null,"c"]""");
        Assert.NotNull(list);
        Assert.Equal(new[] { "a", null, "c" }, list);
    }

    [Fact]
    public void Read_SingleNullElement_IsValid()
    {
        // `[null]` has one element, which satisfies Count >= 1. The element happens to be
        // null — that's a content concern, not a non-empty concern.
        var list = JsonSerializer.Deserialize<NonEmptyEnumerable<string?>>("[null]");
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Null(list.Head);
    }

    [Fact]
    public void Read_NullableValueType_AllowsNulls()
    {
        // int? is literally the nullable-int type — null is a valid value. Rejecting it
        // would make `NonEmptyEnumerable<int?>` unusable for any wire format that encodes
        // a "known absence" as JSON null.
        var list = JsonSerializer.Deserialize<NonEmptyEnumerable<int?>>("[1,null,3]");
        Assert.NotNull(list);
        Assert.Equal(new int?[] { 1, null, 3 }, list);
    }

    [Fact]
    public void Read_NullableValueType_SingleNull_IsValid()
    {
        var list = JsonSerializer.Deserialize<NonEmptyEnumerable<int?>>("[null]");
        Assert.NotNull(list);
        Assert.Single(list);
        Assert.Null(list.Head);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("\"hello\"")]
    [InlineData("42.5")]
    public void Read_NonArrayToken_Throws(string json)
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NonEmptyEnumerable<int>>(json));
    }

    [Fact]
    public void Read_OfPositiveInt_InvalidInner_Throws()
    {
        // The inner Positive<int> converter rejects non-positive numbers — its JsonException
        // must bubble through the outer array converter so an invalid element can never
        // sneak into a NonEmptyEnumerable<Positive<int>>.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NonEmptyEnumerable<Positive<int>>>("[1,-5,3]"));

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NonEmptyEnumerable<Positive<int>>>("[0]"));
    }

    [Fact]
    public void Read_OfPositiveInt_Valid_Deserializes()
    {
        var list = JsonSerializer.Deserialize<NonEmptyEnumerable<Positive<int>>>("[1,2,3]");
        Assert.NotNull(list);
        Assert.Equal(new[] { 1, 2, 3 }, list.Select(p => p.Value));
    }

    // ── Write ───────────────────────────────────────────────────────────

    [Fact]
    public void Write_EmitsJsonArray()
    {
        var list = NonEmptyEnumerable.Of(1, 2, 3);
        Assert.Equal("[1,2,3]", JsonSerializer.Serialize(list));
    }

    [Fact]
    public void Write_Null_EmitsJsonNull()
    {
        Assert.Equal("null", JsonSerializer.Serialize<NonEmptyEnumerable<int>?>(null));
    }

    // ── Roundtrip ───────────────────────────────────────────────────────

    [Property]
    public void Roundtrip_Int(NonEmptyEnumerable<int> list)
    {
        var json = JsonSerializer.Serialize(list);
        var back = JsonSerializer.Deserialize<NonEmptyEnumerable<int>>(json);
        Assert.Equal(list, back);
    }

    [Property]
    public void Roundtrip_NonEmptyString(NonEmptyString[] values)
    {
        // Property generator may hand us an empty array — short-circuit since the
        // property is only about the wire-level round-trip of a non-empty list.
        if (values.Length == 0) return;

        var list = NonEmptyEnumerable.Create(values);
        var json = JsonSerializer.Serialize(list);
        var back = JsonSerializer.Deserialize<NonEmptyEnumerable<NonEmptyString>>(json);
        Assert.Equal(list, back);
    }

    // ── Interop with strong-type converters ─────────────────────────────

    [Fact]
    public void Read_OfNonEmptyString_UsesInnerConverter()
    {
        var list = JsonSerializer.Deserialize<NonEmptyEnumerable<NonEmptyString>>("""["a","b"]""");
        Assert.NotNull(list);
        Assert.Equal(new[] { "a", "b" }, list.Select(n => n.Value));
    }

    [Fact]
    public void Read_OfNonEmptyString_InvalidInner_Throws()
    {
        // The inner NonEmptyString converter rejects whitespace, and its JsonException
        // must propagate through the outer array converter.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<NonEmptyEnumerable<NonEmptyString>>("""["a","   "]"""));
    }

    [Fact]
    public void Write_OfNonEmptyString_UsesInnerConverter()
    {
        var list = NonEmptyEnumerable.Of(
            NonEmptyString.Create("a"),
            NonEmptyString.Create("b"));
        Assert.Equal("""["a","b"]""", JsonSerializer.Serialize(list));
    }
}

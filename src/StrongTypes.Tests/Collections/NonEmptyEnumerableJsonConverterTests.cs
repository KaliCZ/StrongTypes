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
        var list = NonEmptyEnumerable.Create(1, 2, 3);
        Assert.Equal("[1,2,3]", JsonSerializer.Serialize(list));
    }

    [Fact]
    public void Write_Null_EmitsJsonNull()
    {
        Assert.Equal("null", JsonSerializer.Serialize<NonEmptyEnumerable<int>?>(null));
    }

    // ── Roundtrip ───────────────────────────────────────────────────────
    //
    // Round-trip tests are string-anchored: we start from a canonical JSON string,
    // deserialize it, re-serialize, and assert the bytes are identical. That proves
    // the wire contract survives a pass through the converter in both directions and
    // that serialization is idempotent. Specific hand-written examples document the
    // canonical form; the property tests derive the canonical form from a generated
    // value and then assert the same string-stability property.

    [Theory]
    [InlineData("[1]")]
    [InlineData("[1,2,3]")]
    [InlineData("[-5,0,5]")]
    public void Roundtrip_Int_FromCanonicalJson(string json)
    {
        var list = JsonSerializer.Deserialize<NonEmptyEnumerable<int>>(json);
        Assert.Equal(json, JsonSerializer.Serialize(list));
    }

    [Theory]
    [InlineData("""["a"]""")]
    [InlineData("""["a","b","c"]""")]
    public void Roundtrip_NonEmptyString_FromCanonicalJson(string json)
    {
        var list = JsonSerializer.Deserialize<NonEmptyEnumerable<NonEmptyString>>(json);
        Assert.Equal(json, JsonSerializer.Serialize(list));
    }

    [Property]
    public void Roundtrip_Int(NonEmptyEnumerable<int> list)
    {
        // The generated value supplies a canonical JSON string via the first serialize;
        // deserializing and re-serializing must reproduce that string unchanged.
        var canonical = JsonSerializer.Serialize(list);
        var back = JsonSerializer.Deserialize<NonEmptyEnumerable<int>>(canonical);
        Assert.Equal(canonical, JsonSerializer.Serialize(back));
    }

    [Property]
    public void Roundtrip_NonEmptyString(NonEmptyString[] values)
    {
        if (values.Length == 0) return;

        var list = NonEmptyEnumerable.Create(values);
        var canonical = JsonSerializer.Serialize(list);
        var back = JsonSerializer.Deserialize<NonEmptyEnumerable<NonEmptyString>>(canonical);
        Assert.Equal(canonical, JsonSerializer.Serialize(back));
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
        var list = NonEmptyEnumerable.Create(
            NonEmptyString.Create("a"),
            NonEmptyString.Create("b"));
        Assert.Equal("""["a","b"]""", JsonSerializer.Serialize(list));
    }

    // ── Interface (INonEmptyEnumerable<T>) ──────────────────────────────
    // STJ matches converters by exact declared type, so the factory has to register against
    // both the concrete class and the interface. Before interface support, deserializing
    // INonEmptyEnumerable<int> threw NotSupportedException ("collection type is abstract").

    [Fact]
    public void Interface_Read_Array_DeserializesToNonEmpty()
    {
        var list = JsonSerializer.Deserialize<INonEmptyEnumerable<int>>("[1,2,3]");
        Assert.NotNull(list);
        Assert.IsType<NonEmptyEnumerable<int>>(list);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void Interface_Read_Null_ReturnsNull()
    {
        var list = JsonSerializer.Deserialize<INonEmptyEnumerable<int>>("null");
        Assert.Null(list);
    }

    [Fact]
    public void Interface_Read_EmptyArray_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<INonEmptyEnumerable<int>>("[]"));
    }

    [Fact]
    public void Interface_Read_NonArray_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<INonEmptyEnumerable<int>>("42"));
    }

    [Fact]
    public void Interface_Read_OfNonEmptyString_UsesInnerConverter()
    {
        var list = JsonSerializer.Deserialize<INonEmptyEnumerable<NonEmptyString>>("""["a","b"]""");
        Assert.NotNull(list);
        Assert.Equal(new[] { "a", "b" }, list.Select(n => n.Value));
    }

    [Fact]
    public void Interface_Write_EmitsJsonArray()
    {
        INonEmptyEnumerable<int> list = NonEmptyEnumerable.Create(1, 2, 3);
        Assert.Equal("[1,2,3]", JsonSerializer.Serialize(list));
    }

    [Fact]
    public void Interface_Write_Null_EmitsJsonNull()
    {
        Assert.Equal("null", JsonSerializer.Serialize<INonEmptyEnumerable<int>?>(null));
    }

    [Fact]
    public void Interface_Roundtrip_FromCanonicalJson()
    {
        const string json = "[10,20,30]";
        var list = JsonSerializer.Deserialize<INonEmptyEnumerable<int>>(json);
        Assert.Equal(json, JsonSerializer.Serialize(list));
    }
}

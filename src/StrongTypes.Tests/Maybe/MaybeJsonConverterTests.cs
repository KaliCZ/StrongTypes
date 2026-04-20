#nullable enable

using System.Text.Json;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class MaybeJsonConverterTests
{
    // ── Read: all three accepted shapes map to the expected Maybe ───────

    [Fact]
    public void Read_EmptyObject_IsEmpty()
    {
        var m = JsonSerializer.Deserialize<Maybe<int>>("{}");
        Assert.False(m.HasValue);
    }

    [Fact]
    public void Read_ValueNull_IsEmpty()
    {
        var m = JsonSerializer.Deserialize<Maybe<int>>("""{"Value":null}""");
        Assert.False(m.HasValue);
    }

    [Fact]
    public void Read_ValueSet_IsSome()
    {
        var m = JsonSerializer.Deserialize<Maybe<int>>("""{"Value":4}""");
        Assert.True(m.HasValue);
        Assert.Equal(4, m.Value);
    }

    [Fact]
    public void Read_CaseInsensitivePropertyName()
    {
        var m = JsonSerializer.Deserialize<Maybe<int>>("""{"value":4}""");
        Assert.True(m.HasValue);
        Assert.Equal(4, m.Value);
    }

    [Fact]
    public void Read_UnknownProperty_Ignored()
    {
        var m = JsonSerializer.Deserialize<Maybe<int>>("""{"Extra":"x","Value":7}""");
        Assert.Equal(Maybe<int>.Some(7), m);
    }

    [Fact]
    public void Read_ReferenceType_ValueSet_IsSome()
    {
        var m = JsonSerializer.Deserialize<Maybe<string>>("""{"Value":"hello"}""");
        Assert.Equal(Maybe<string>.Some("hello"), m);
    }

    [Fact]
    public void Read_ReferenceType_EmptyObject_IsEmpty()
    {
        var m = JsonSerializer.Deserialize<Maybe<string>>("{}");
        Assert.False(m.HasValue);
    }

    // ── Write: always emits {"Value": ...} shape ────────────────────────

    [Fact]
    public void Write_Some_EmitsValueProperty()
    {
        var json = JsonSerializer.Serialize(Maybe<int>.Some(42));
        Assert.Equal("""{"Value":42}""", json);
    }

    [Fact]
    public void Write_Empty_EmitsValueNull()
    {
        var json = JsonSerializer.Serialize(Maybe<int>.None);
        Assert.Equal("""{"Value":null}""", json);
    }

    [Fact]
    public void Write_ReferenceType_Some()
    {
        var json = JsonSerializer.Serialize(Maybe<string>.Some("hi"));
        Assert.Equal("""{"Value":"hi"}""", json);
    }

    // ── Roundtrip (property-based) ──────────────────────────────────────

    [Property]
    public void Roundtrip_Int(Maybe<int> m)
    {
        var json = JsonSerializer.Serialize(m);
        Assert.Equal(m, JsonSerializer.Deserialize<Maybe<int>>(json));
    }

    [Property]
    public void Roundtrip_String(Maybe<string> m)
    {
        var json = JsonSerializer.Serialize(m);
        Assert.Equal(m, JsonSerializer.Deserialize<Maybe<string>>(json));
    }

    // ── Interop with strong-type converters ─────────────────────────────

    [Fact]
    public void Read_MaybeOfNonEmptyString_UsesItsConverter()
    {
        var m = JsonSerializer.Deserialize<Maybe<NonEmptyString>>("""{"Value":"abc"}""");
        Assert.True(m.HasValue);
        Assert.Equal(NonEmptyString.Create("abc"), m.Value);
    }

    [Fact]
    public void Write_MaybeOfNonEmptyString_UsesItsConverter()
    {
        var json = JsonSerializer.Serialize(Maybe<NonEmptyString>.Some(NonEmptyString.Create("abc")));
        Assert.Equal("""{"Value":"abc"}""", json);
    }
}

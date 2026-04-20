#nullable enable

using System.Text.Json;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class MaybeJsonConverterTests
{
    // ── Maybe<T>: flat wire format (raw value, not an object wrapper) ────

    [Fact]
    public void Read_Null_IsNone()
    {
        var m = JsonSerializer.Deserialize<Maybe<int>>("null");
        Assert.False(m.HasValue);
    }

    [Fact]
    public void Read_Scalar_IsSome()
    {
        var m = JsonSerializer.Deserialize<Maybe<int>>("4");
        Assert.Equal(Maybe<int>.Some(4), m);
    }

    [Fact]
    public void Read_ReferenceType_Scalar_IsSome()
    {
        var m = JsonSerializer.Deserialize<Maybe<string>>("\"hello\"");
        Assert.Equal(Maybe<string>.Some("hello"), m);
    }

    [Fact]
    public void Read_ReferenceType_Null_IsNone()
    {
        var m = JsonSerializer.Deserialize<Maybe<string>>("null");
        Assert.False(m.HasValue);
    }

    [Fact]
    public void Write_Some_EmitsRawValue()
    {
        var json = JsonSerializer.Serialize(Maybe<int>.Some(42));
        Assert.Equal("42", json);
    }

    [Fact]
    public void Write_None_EmitsNull()
    {
        var json = JsonSerializer.Serialize(Maybe<int>.None);
        Assert.Equal("null", json);
    }

    [Fact]
    public void Write_ReferenceType_Some()
    {
        var json = JsonSerializer.Serialize(Maybe<string>.Some("hi"));
        Assert.Equal("\"hi\"", json);
    }

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

    // ── Interop with strong-type inner converters ───────────────────────

    [Fact]
    public void Read_MaybeOfNonEmptyString_UsesItsConverter()
    {
        var m = JsonSerializer.Deserialize<Maybe<NonEmptyString>>("\"abc\"");
        Assert.Equal(Maybe<NonEmptyString>.Some(NonEmptyString.Create("abc")), m);
    }

    [Fact]
    public void Write_MaybeOfNonEmptyString_UsesItsConverter()
    {
        var json = JsonSerializer.Serialize(Maybe<NonEmptyString>.Some(NonEmptyString.Create("abc")));
        Assert.Equal("\"abc\"", json);
    }

    [Fact]
    public void Read_MaybeOfNonEmptyString_InvalidValue_Throws()
    {
        // The inner NonEmptyString converter rejects whitespace/empty, and that
        // JsonException must surface — we don't want a silent fallback to None.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Maybe<NonEmptyString>>("\"   \""));
    }

    [Fact]
    public void Read_MaybeOfPositiveInt_Some()
    {
        var m = JsonSerializer.Deserialize<Maybe<Positive<int>>>("7");
        Assert.Equal(Maybe<Positive<int>>.Some(Positive<int>.Create(7)), m);
    }

    [Fact]
    public void Read_MaybeOfPositiveInt_Null_IsNone()
    {
        var m = JsonSerializer.Deserialize<Maybe<Positive<int>>>("null");
        Assert.False(m.HasValue);
    }

    [Fact]
    public void Write_MaybeOfPositiveInt_Some()
    {
        var json = JsonSerializer.Serialize(Maybe<Positive<int>>.Some(Positive<int>.Create(7)));
        Assert.Equal("7", json);
    }

    [Fact]
    public void Read_MaybeOfPositiveInt_InvalidValue_Throws()
    {
        // Zero violates the Positive invariant — the inner converter throws and
        // that exception propagates through the Maybe converter.
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Maybe<Positive<int>>>("0"));

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Maybe<Positive<int>>>("-5"));
    }

    [Property]
    public void Roundtrip_PositiveInt(Maybe<Positive<int>> m)
    {
        var json = JsonSerializer.Serialize(m);
        Assert.Equal(m, JsonSerializer.Deserialize<Maybe<Positive<int>>>(json));
    }

    // ── Maybe<T>?: tri-state for HTTP PATCH ─────────────────────────────
    //
    // STJ's default Nullable<T> handling intercepts JSON null before the
    // underlying converter runs, which would collapse null into a null
    // nullable. Registering MaybeJsonConverterFactory in Options.Converters
    // provides an explicit Nullable<Maybe<T>> converter that keeps the three
    // states distinct.

    private static JsonSerializerOptions TriStateOptions() => new()
    {
        Converters = { new MaybeJsonConverterFactory() }
    };

    [Fact]
    public void ReadNullable_JsonNull_IsPresentNone()
    {
        var m = JsonSerializer.Deserialize<Maybe<int>?>("null", TriStateOptions());
        Assert.True(m.HasValue);
        Assert.Equal(Maybe<int>.None, m!.Value);
    }

    [Fact]
    public void ReadNullable_JsonValue_IsPresentSome()
    {
        var m = JsonSerializer.Deserialize<Maybe<int>?>("7", TriStateOptions());
        Assert.True(m.HasValue);
        Assert.Equal(Maybe<int>.Some(7), m!.Value);
    }

    [Fact]
    public void WriteNullable_Null_EmitsJsonNull()
    {
        var json = JsonSerializer.Serialize<Maybe<int>?>(null, TriStateOptions());
        Assert.Equal("null", json);
    }

    [Fact]
    public void WriteNullable_None_EmitsJsonNull()
    {
        var json = JsonSerializer.Serialize<Maybe<int>?>(Maybe<int>.None, TriStateOptions());
        Assert.Equal("null", json);
    }

    [Fact]
    public void WriteNullable_Some_EmitsRawValue()
    {
        var json = JsonSerializer.Serialize<Maybe<int>?>(Maybe<int>.Some(42), TriStateOptions());
        Assert.Equal("42", json);
    }

    // The absent/null/value distinction only matters in a parent object
    // context, because "absent" means STJ never invokes the converter for
    // that property. Two records cover both shapes end-to-end — Maybe<T>?
    // for the tri-state PATCH case and plain Maybe<T> for the flat case.
    private sealed record PatchDto(int? Value, Maybe<int>? NullableValue);
    private sealed record PlainDto(Maybe<int> NullableValue);

    // ── Maybe<T>? as a property: read ────────────────────────────────────

    [Fact]
    public void ReadPatchDto_PropertyAbsent_StaysNull()
    {
        var dto = JsonSerializer.Deserialize<PatchDto>("""{"Value":1}""", TriStateOptions());
        Assert.NotNull(dto);
        Assert.Equal(1, dto!.Value);
        Assert.Null(dto.NullableValue);
    }

    [Fact]
    public void ReadPatchDto_PropertyJsonNull_IsPresentNone()
    {
        var dto = JsonSerializer.Deserialize<PatchDto>("""{"NullableValue":null}""", TriStateOptions());
        Assert.NotNull(dto);
        Assert.Equal(Maybe<int>.None, dto!.NullableValue);
    }

    [Fact]
    public void ReadPatchDto_PropertyJsonValue_IsPresentSome()
    {
        var dto = JsonSerializer.Deserialize<PatchDto>("""{"NullableValue":42}""", TriStateOptions());
        Assert.NotNull(dto);
        Assert.Equal(Maybe<int>.Some(42), dto!.NullableValue);
    }

    // ── Maybe<T>? as a property: write ───────────────────────────────────

    [Fact]
    public void WritePatchDto_PropertyNull_EmitsJsonNull()
    {
        var json = JsonSerializer.Serialize(new PatchDto(null, null), TriStateOptions());
        Assert.Equal("""{"Value":null,"NullableValue":null}""", json);
    }

    [Fact]
    public void WritePatchDto_PropertyNone_EmitsJsonNull()
    {
        var json = JsonSerializer.Serialize(new PatchDto(null, Maybe<int>.None), TriStateOptions());
        Assert.Equal("""{"Value":null,"NullableValue":null}""", json);
    }

    [Fact]
    public void WritePatchDto_PropertySome_EmitsRawValue()
    {
        var json = JsonSerializer.Serialize(new PatchDto(null, Maybe<int>.Some(42)), TriStateOptions());
        Assert.Equal("""{"Value":null,"NullableValue":42}""", json);
    }

    // ── Maybe<T> as a property (non-nullable): read ──────────────────────
    // Absent and null both land on Maybe<T>.None — the flat shape collapses
    // them, which is why PATCH DTOs use Maybe<T>? instead.

    [Fact]
    public void ReadPlainDto_PropertyAbsent_IsNone()
    {
        var dto = JsonSerializer.Deserialize<PlainDto>("{}", TriStateOptions());
        Assert.NotNull(dto);
        Assert.Equal(Maybe<int>.None, dto!.NullableValue);
    }

    [Fact]
    public void ReadPlainDto_PropertyJsonNull_IsNone()
    {
        var dto = JsonSerializer.Deserialize<PlainDto>("""{"NullableValue":null}""", TriStateOptions());
        Assert.NotNull(dto);
        Assert.Equal(Maybe<int>.None, dto!.NullableValue);
    }

    [Fact]
    public void ReadPlainDto_PropertyJsonValue_IsSome()
    {
        var dto = JsonSerializer.Deserialize<PlainDto>("""{"NullableValue":42}""", TriStateOptions());
        Assert.NotNull(dto);
        Assert.Equal(Maybe<int>.Some(42), dto!.NullableValue);
    }

    // ── Maybe<T> as a property (non-nullable): write ─────────────────────

    [Fact]
    public void WritePlainDto_PropertyNone_EmitsJsonNull()
    {
        var json = JsonSerializer.Serialize(new PlainDto(Maybe<int>.None), TriStateOptions());
        Assert.Equal("""{"NullableValue":null}""", json);
    }

    [Fact]
    public void WritePlainDto_PropertySome_EmitsRawValue()
    {
        var json = JsonSerializer.Serialize(new PlainDto(Maybe<int>.Some(42)), TriStateOptions());
        Assert.Equal("""{"NullableValue":42}""", json);
    }
}

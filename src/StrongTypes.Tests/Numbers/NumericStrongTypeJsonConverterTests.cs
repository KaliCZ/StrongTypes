using System.Text.Json;
using Xunit;

namespace StrongTypes.Tests;

public sealed class NumericStrongTypeJsonConverterTests
{
    private sealed record Holder(Positive<int> Value);

    [Theory]
    [InlineData("\"abc\"")] // wrong JSON type
    [InlineData("null")]    // null for a non-nullable struct
    [InlineData("0")]       // fails the positive invariant
    [InlineData("-5")]
    public void Deserialize_InvalidValue_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Positive<int>>(json));
    }

    // Every invalid kind must surface under the property's path ("$.Value"), not
    // the document root — the nested Deserialize<T> would otherwise lose position
    // on type-mismatch/null. Locks the converter rethrow that keeps the path.
    [Theory]
    [InlineData("""{"Value":"abc"}""")]
    [InlineData("""{"Value":null}""")]
    [InlineData("""{"Value":0}""")]
    public void Deserialize_InvalidValueInObject_ReportsPropertyPath(string json)
    {
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Holder>(json));
        Assert.Equal("$.Value", ex.Path);
    }
}

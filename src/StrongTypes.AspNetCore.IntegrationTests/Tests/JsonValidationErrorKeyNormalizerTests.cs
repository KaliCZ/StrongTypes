using Xunit;

namespace StrongTypes.AspNetCore.IntegrationTests.Tests;

public sealed class JsonValidationErrorKeyNormalizerTests
{
    [Theory]
    [InlineData("$.value", JsonErrorKeyCasing.PascalCase, "Value")]
    [InlineData("$.value", JsonErrorKeyCasing.CamelCase, "value")]
    [InlineData("$.value", JsonErrorKeyCasing.StripOnly, "value")]
    [InlineData("$.Value", JsonErrorKeyCasing.CamelCase, "value")]
    [InlineData("$.nullableValue", JsonErrorKeyCasing.PascalCase, "NullableValue")]
    // Nested and array segments: every name re-cased, indexers preserved.
    [InlineData("$.items[0].name", JsonErrorKeyCasing.PascalCase, "Items[0].Name")]
    [InlineData("$.items[0].name", JsonErrorKeyCasing.CamelCase, "items[0].name")]
    [InlineData("$.items[0].name", JsonErrorKeyCasing.StripOnly, "items[0].name")]
    // Root array element has no leading name to re-case.
    [InlineData("$[0]", JsonErrorKeyCasing.PascalCase, "[0]")]
    [InlineData("$", JsonErrorKeyCasing.PascalCase, "")]
    // Keys without the JSON root come from model binding and pass through untouched.
    [InlineData("Value", JsonErrorKeyCasing.CamelCase, "Value")]
    [InlineData("body", JsonErrorKeyCasing.PascalCase, "body")]
    public void Normalize_ProducesExpectedKey(string input, JsonErrorKeyCasing casing, string expected)
    {
        Assert.Equal(expected, JsonValidationErrorKeyNormalizer.Normalize(input, casing));
    }
}

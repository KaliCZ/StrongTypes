using System.Text.Json;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Each navigator asserts a specific shape and returns the inner layer, so the choice of method at a call site is
/// itself an assertion.
/// </summary>
internal static class SchemaNavigation
{
    internal static JsonElement FollowRef(JsonElement doc, JsonElement schema)
    {
        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.True(schema.TryGetProperty("$ref", out var refProp), "$ref is missing");
        var path = refProp.GetString()!;
        const string prefix = "#/components/schemas/";
        Assert.StartsWith(prefix, path);
        var name = path[prefix.Length..];
        return doc.GetProperty("components").GetProperty("schemas").GetProperty(name);
    }

    /// <summary>
    /// Follows <c>$ref</c> and single-element <c>allOf</c>; never a nullable union — those are version-specific and
    /// must be unwrapped explicitly.
    /// </summary>
    internal static JsonElement Resolve(JsonElement doc, JsonElement schema)
    {
        while (true)
        {
            if (schema.ValueKind != JsonValueKind.Object) return schema;

            if (schema.TryGetProperty("$ref", out _))
            {
                schema = FollowRef(doc, schema);
                continue;
            }

            if (schema.TryGetProperty("allOf", out var allOf)
                && allOf.ValueKind == JsonValueKind.Array
                && allOf.GetArrayLength() == 1)
            {
                schema = allOf[0];
                continue;
            }

            return schema;
        }
    }

    internal static JsonElement RequestSchema(JsonElement doc, string path, string method = "post")
        => doc.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");

    internal static JsonElement ParameterSchema(JsonElement doc, string path, string parameterName, string method = "get")
    {
        var operation = doc.GetProperty("paths").GetProperty(path).GetProperty(method);
        Assert.True(operation.TryGetProperty("parameters", out var parameters), $"operation {method} {path} has no parameters");
        foreach (var p in parameters.EnumerateArray())
        {
            if (p.GetProperty("name").GetString() == parameterName)
            {
                Assert.True(p.TryGetProperty("schema", out var schema), $"parameter {parameterName} on {method} {path} has no schema");
                return schema;
            }
        }
        Assert.Fail($"parameter {parameterName} not found on {method} {path}");
        return default;
    }

    /// <summary>Accepts either form content type — the pipelines differ in which they emit.</summary>
    internal static JsonElement FormRequestSchema(JsonElement doc, string path, string method = "post")
    {
        var content = doc.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("requestBody").GetProperty("content");
        foreach (var contentType in new[] { "multipart/form-data", "application/x-www-form-urlencoded" })
        {
            if (content.TryGetProperty(contentType, out var ct))
            {
                return ct.GetProperty("schema");
            }
        }
        Assert.Fail($"no form content-type found on {method} {path}");
        return default;
    }

    internal static JsonElement Property(JsonElement schema, string propertyName)
        => schema.GetProperty("properties").GetProperty(propertyName);

    internal static void AssertInlineSchema(JsonElement schema)
    {
        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.False(schema.TryGetProperty("$ref", out _), "expected inline schema, found $ref");
        Assert.False(schema.TryGetProperty("allOf", out _), "expected inline schema, found allOf");
    }

    /// <summary>
    /// Strict deep-equality: the same property-name set on every object, same array length and order, numbers
    /// compared as <see cref="decimal"/> so <c>1</c> matches <c>1.0</c>.
    /// </summary>
    internal static void AssertJsonEquals(JsonElement actual, string expectedJson)
    {
        using var doc = JsonDocument.Parse(expectedJson);
        AssertJsonEqualsCore(doc.RootElement, actual, path: "$");
    }

    // Swashbuckle emits these 3.0 keywords at their false default where Microsoft omits them; both are wire-equivalent to absent.
    private static readonly HashSet<string> s_falseDefaultKeywords = new(StringComparer.Ordinal)
    {
        "exclusiveMinimum",
        "exclusiveMaximum",
    };

    private static void AssertJsonEqualsCore(JsonElement expected, JsonElement actual, string path)
    {
        if (expected.ValueKind != actual.ValueKind)
            Assert.Fail($"at {path}: expected {expected.ValueKind} but got {actual.ValueKind}\n  expected: {expected.GetRawText()}\n  actual:   {actual.GetRawText()}");

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedKeys = expected.EnumerateObject().Select(p => p.Name).Order().ToArray();
                var actualKeys = actual.EnumerateObject()
                    .Where(p => !IsRedundantDefault(p, expected))
                    .Select(p => p.Name).Order().ToArray();
                if (!expectedKeys.SequenceEqual(actualKeys))
                    Assert.Fail($"at {path}: property-name set differs\n  expected: [{string.Join(", ", expectedKeys)}]\n  actual:   [{string.Join(", ", actualKeys)}]\n  actual schema: {actual.GetRawText()}");
                foreach (var prop in expected.EnumerateObject())
                    AssertJsonEqualsCore(prop.Value, actual.GetProperty(prop.Name), $"{path}.{prop.Name}");
                break;

            case JsonValueKind.Array:
                if (expected.GetArrayLength() != actual.GetArrayLength())
                    Assert.Fail($"at {path}: array length differs (expected {expected.GetArrayLength()}, got {actual.GetArrayLength()})\n  expected: {expected.GetRawText()}\n  actual:   {actual.GetRawText()}");
                for (var i = 0; i < expected.GetArrayLength(); i++)
                    AssertJsonEqualsCore(expected[i], actual[i], $"{path}[{i}]");
                break;

            case JsonValueKind.String:
                Assert.Equal(expected.GetString(), actual.GetString());
                break;

            case JsonValueKind.Number:
                Assert.Equal(expected.GetDecimal(), actual.GetDecimal());
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                // the kind is the whole value, and kinds were already compared
                break;
        }
    }

    private static bool IsRedundantDefault(JsonProperty prop, JsonElement expected)
        => s_falseDefaultKeywords.Contains(prop.Name)
           && prop.Value.ValueKind == JsonValueKind.False
           && !expected.TryGetProperty(prop.Name, out _);
}

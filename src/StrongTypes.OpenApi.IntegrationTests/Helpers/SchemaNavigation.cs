using System.Text.Json;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Navigation primitives over an OpenAPI document. Each method asserts a
/// specific shape and returns the inner layer; the choice of method at
/// the call site is itself an assertion. Composing them (e.g.
/// <c>FollowRef(doc, UnwrapNullableProperty(x))</c>) expresses the full
/// expected wire structure of a property's schema.
/// </summary>
internal static class SchemaNavigation
{
    /// <summary>
    /// Resolves a strict <c>$ref</c> to the referenced component schema.
    /// Fails the test if <paramref name="schema"/> is not an object with
    /// a <c>$ref</c> targeting <c>#/components/schemas/&lt;name&gt;</c>.
    /// </summary>
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
    /// Walks past shape-agnostic indirection layers and returns the
    /// underlying schema. Follows <c>$ref</c> and single-element
    /// <c>allOf</c>; never walks a nullable union (those are
    /// version-specific and should be unwrapped explicitly).
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

    /// <summary>Returns the request-body schema for the given path/method.</summary>
    internal static JsonElement RequestSchema(JsonElement doc, string path, string method = "post")
        => doc.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("requestBody").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");

    /// <summary>
    /// Returns the schema attached to the named parameter on the given
    /// path/method. Fails the test if the parameter (or its <c>schema</c>) is
    /// missing.
    /// </summary>
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

    /// <summary>
    /// Returns the form request-body schema for the given path/method. Form
    /// bodies are encoded as <c>application/x-www-form-urlencoded</c> or
    /// <c>multipart/form-data</c> — both are accepted; the test uses
    /// whichever the pipeline emitted.
    /// </summary>
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

    /// <summary>Returns the schema for a named property of an object schema.</summary>
    internal static JsonElement Property(JsonElement schema, string propertyName)
        => schema.GetProperty("properties").GetProperty(propertyName);

    /// <summary>
    /// Asserts the schema is fully inlined: keywords sit directly on the
    /// schema object, not behind a <c>$ref</c> or an <c>allOf</c>.
    /// </summary>
    internal static void AssertInlineSchema(JsonElement schema)
    {
        Assert.Equal(JsonValueKind.Object, schema.ValueKind);
        Assert.False(schema.TryGetProperty("$ref", out _), "expected inline schema, found $ref");
        Assert.False(schema.TryGetProperty("allOf", out _), "expected inline schema, found allOf");
    }

    /// <summary>
    /// Asserts that <paramref name="actual"/> deep-equals
    /// <paramref name="expectedJson"/> — same property-name set on every
    /// object, same array length and order, same primitive values
    /// (numbers compared as <see cref="decimal"/> so <c>1</c> matches
    /// <c>1.0</c>). Property order on objects is ignored. This pins the
    /// full emitted shape so that an unexpected keyword (a wrong
    /// <c>format</c>, a stray <c>nullable</c>, some new <c>x-…</c>
    /// annotation, …) fails the test instead of silently passing.
    /// </summary>
    internal static void AssertJsonEquals(JsonElement actual, string expectedJson)
    {
        using var doc = JsonDocument.Parse(expectedJson);
        AssertJsonEqualsCore(doc.RootElement, actual, path: "$");
    }

    private static void AssertJsonEqualsCore(JsonElement expected, JsonElement actual, string path)
    {
        if (expected.ValueKind != actual.ValueKind)
            Assert.Fail($"at {path}: expected {expected.ValueKind} but got {actual.ValueKind}\n  expected: {expected.GetRawText()}\n  actual:   {actual.GetRawText()}");

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedKeys = expected.EnumerateObject().Select(p => p.Name).Order().ToArray();
                var actualKeys = actual.EnumerateObject().Select(p => p.Name).Order().ToArray();
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
                // Kinds already verified equal above and these have no
                // payload — the kind IS the value.
                break;
        }
    }
}

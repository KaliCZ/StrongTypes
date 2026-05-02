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
}

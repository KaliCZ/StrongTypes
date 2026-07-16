using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// The pipelines emit a property's nullability in several shapes — a <c>oneOf</c>/<c>anyOf</c> union with a null
/// branch, a flat <c>nullable: true</c> (3.0) or <c>"null"</c> type-array member (3.1), a single-element <c>allOf</c>,
/// or no wrapper at all when nullability rides solely on <c>required</c>.
/// </summary>
internal static class NullableUnwrap
{
    /// <summary>
    /// True iff <paramref name="branch"/> is exactly the singleton <c>{ "nullable": true }</c> — a nullable schema
    /// carrying its own constraints is not a null marker.
    /// </summary>
    internal static bool IsNullBranch3_0(JsonElement branch) =>
        branch.TryGetProperty("nullable", out var n)
        && n.ValueKind == JsonValueKind.True
        && branch.EnumerateObject().Count() == 1;

    /// <summary>True iff <paramref name="branch"/> is exactly the singleton <c>{ "type": "null" }</c>.</summary>
    internal static bool IsNullBranch3_1(JsonElement branch) =>
        branch.TryGetProperty("type", out var t)
        && t.ValueKind == JsonValueKind.String
        && t.GetString() == "null"
        && branch.EnumerateObject().Count() == 1;

    /// <summary>
    /// Dispatches strictly — a document accepts only its own version's null marker — so cross-version contamination
    /// surfaces instead of silently passing.
    /// </summary>
    internal static bool IsNullBranch(JsonElement branch, OpenApiVersion version) =>
        version == OpenApiVersion.V3_1 ? IsNullBranch3_1(branch) : IsNullBranch3_0(branch);

    /// <summary>Returns the schema behind its nullable wrapper, unchanged when no wrapper layer is present.</summary>
    internal static JsonElement UnwrapNullableProperty(JsonElement schema, OpenApiVersion version)
    {
        AssertVersionMarkers(schema, version);

        if (schema.TryGetProperty("oneOf", out var oneOf) && oneOf.ValueKind == JsonValueKind.Array)
            return UnwrapNullableUnion(schema, "oneOf", version);
        if (schema.TryGetProperty("anyOf", out var anyOf) && anyOf.ValueKind == JsonValueKind.Array)
            return UnwrapNullableUnion(schema, "anyOf", version);
        if (schema.TryGetProperty("allOf", out var allOf) && allOf.ValueKind == JsonValueKind.Array
            && allOf.GetArrayLength() == 1)
            return allOf[0];

        return schema;
    }

    /// <summary>
    /// Asserts the property encodes <c>T?</c> nullability and returns the wire schema with the null marker stripped —
    /// unlike <see cref="UnwrapNullableProperty"/>, a property carrying no null marker fails.
    /// </summary>
    internal static JsonElement AssertNullableAndUnwrap(JsonElement schema, OpenApiVersion version)
    {
        AssertVersionMarkers(schema, version);

        foreach (var keyword in new[] { "oneOf", "anyOf" })
        {
            if (!schema.TryGetProperty(keyword, out var union) || union.ValueKind != JsonValueKind.Array) continue;

            var wire = UnwrapNullableUnion(schema, keyword, version);
            if (version == OpenApiVersion.V3_0)
                Assert.True(IsFlatNullable3_0(schema), $"3.0 union nullable must carry nullable:true: {schema.GetRawText()}");
            else
                Assert.True(
                    union.EnumerateArray().Any(IsNullBranch3_1),
                    $"3.1 union must carry a {{\"type\":\"null\"}} branch: {schema.GetRawText()}");
            return wire;
        }

        if (version == OpenApiVersion.V3_0)
        {
            Assert.True(IsFlatNullable3_0(schema), $"expected nullable:true on a nullable property: {schema.GetRawText()}");
            return RemoveKey(schema, "nullable");
        }

        Assert.True(TypeArrayHasNull(schema), $"expected a \"null\" member in the type array: {schema.GetRawText()}");
        return CollapseNullFromTypeArray(schema);
    }

    private static bool IsFlatNullable3_0(JsonElement schema) =>
        schema.TryGetProperty("nullable", out var n) && n.ValueKind == JsonValueKind.True;

    private static bool TypeArrayHasNull(JsonElement schema) =>
        schema.TryGetProperty("type", out var t)
        && t.ValueKind == JsonValueKind.Array
        && t.EnumerateArray().Any(e => e.ValueKind == JsonValueKind.String && e.GetString() == "null");

    private static JsonElement RemoveKey(JsonElement schema, string key)
    {
        var node = JsonNode.Parse(schema.GetRawText())!.AsObject();
        node.Remove(key);
        return JsonDocument.Parse(node.ToJsonString()).RootElement;
    }

    private static JsonElement CollapseNullFromTypeArray(JsonElement schema)
    {
        var node = JsonNode.Parse(schema.GetRawText())!.AsObject();
        if (node["type"] is JsonArray arr)
        {
            var nonNull = arr.Where(e => e?.GetValue<string>() != "null").Select(e => e!.GetValue<string>()).ToList();
            node["type"] = nonNull.Count == 1
                ? JsonValue.Create(nonNull[0])
                : new JsonArray(nonNull.Select(s => (JsonNode)JsonValue.Create(s)!).ToArray());
        }
        return JsonDocument.Parse(node.ToJsonString()).RootElement;
    }

    private static JsonElement UnwrapNullableUnion(JsonElement schema, string keyword, OpenApiVersion version)
    {
        Assert.True(schema.TryGetProperty(keyword, out var union), $"{keyword} is missing");
        Assert.Equal(JsonValueKind.Array, union.ValueKind);

        JsonElement? nonNull = null;
        foreach (var branch in union.EnumerateArray())
        {
            Assert.Equal(JsonValueKind.Object, branch.ValueKind);
            if (IsNullBranch(branch, version)) continue;
            Assert.True(nonNull is null, $"{keyword} must have exactly one non-null branch");
            nonNull = branch;
        }

        Assert.True(nonNull.HasValue, $"{keyword} has no non-null branch");
        return nonNull.Value;
    }

    private static void AssertVersionMarkers(JsonElement schema, OpenApiVersion version)
    {
        if (version == OpenApiVersion.V3_1)
        {
            Assert.False(
                schema.TryGetProperty("nullable", out _),
                "3.1 schemas must not use the nullable:true marker (3.0 form)");
            return;
        }

        foreach (var key in new[] { "oneOf", "anyOf" })
        {
            if (!schema.TryGetProperty(key, out var union) || union.ValueKind != JsonValueKind.Array) continue;
            foreach (var branch in union.EnumerateArray())
            {
                Assert.False(
                    IsNullBranch3_1(branch),
                    $"3.0 schemas must not contain a {{\"type\":\"null\"}} branch ({key}); that's the 3.1 form");
            }
        }
    }
}

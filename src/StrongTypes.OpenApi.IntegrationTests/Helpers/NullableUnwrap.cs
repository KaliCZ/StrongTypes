using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Version-aware nullable-wrapper unwrapping. ASP.NET Core OpenAPI
/// pipelines emit a property's CLR-level nullability in several
/// shapes (<c>oneOf+nullable:true</c>, <c>anyOf+{type:"null"}</c>,
/// single-element <c>allOf</c>, or no wrapper at all when the
/// pipeline relies solely on <c>required</c>). This helper walks
/// past whichever wrapper is present and returns the inner schema
/// for downstream navigation, asserting strict version markers
/// along the way.
/// </summary>
internal static class NullableUnwrap
{
    /// <summary>
    /// True iff <paramref name="branch"/> is the OpenAPI 3.0 null marker:
    /// a singleton object <c>{ "nullable": true }</c> with no other
    /// keywords. Pins the branch as encoding "or null" inside a
    /// <c>oneOf</c>/<c>anyOf</c> union; the singleton check excludes
    /// nullable schemas that also carry their own constraints.
    /// </summary>
    internal static bool IsNullBranch3_0(JsonElement branch) =>
        branch.TryGetProperty("nullable", out var n)
        && n.ValueKind == JsonValueKind.True
        && branch.EnumerateObject().Count() == 1;

    /// <summary>
    /// True iff <paramref name="branch"/> is the OpenAPI 3.1 null marker:
    /// a singleton object <c>{ "type": "null" }</c> with no other
    /// keywords. The 3.1 spec dropped the <c>nullable</c> keyword and
    /// uses a <c>null</c>-typed branch instead.
    /// </summary>
    internal static bool IsNullBranch3_1(JsonElement branch) =>
        branch.TryGetProperty("type", out var t)
        && t.ValueKind == JsonValueKind.String
        && t.GetString() == "null"
        && branch.EnumerateObject().Count() == 1;

    /// <summary>
    /// True iff <paramref name="branch"/> is the null marker for the
    /// document's declared OpenAPI version. Dispatches strictly: a 3.0
    /// document accepts only the 3.0 marker, a 3.1 document accepts
    /// only the 3.1 marker, so cross-version contamination surfaces as
    /// an unhandled branch rather than silently passing through.
    /// </summary>
    internal static bool IsNullBranch(JsonElement branch, OpenApiVersion version) =>
        version == OpenApiVersion.V3_1 ? IsNullBranch3_1(branch) : IsNullBranch3_0(branch);

    /// <summary>
    /// Walks past the nullable wrapper layer (whichever form the
    /// pipeline used) and returns the inner schema. Asserts the
    /// version-marker partition first: a 3.0 schema must not carry the
    /// <c>nullable</c> keyword nowhere; a 3.1 schema must not carry a
    /// <c>{"type":"null"}</c> branch in any union. Returns the schema
    /// unchanged when the property has no wrapper layer (Swashbuckle's
    /// typical form for value-typed and same-shape-as-non-nullable
    /// cases, where nullability is encoded solely via <c>required</c>).
    /// </summary>
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
    /// Asserts the property actually encodes <c>T?</c> nullability — in
    /// whichever shape the pipeline uses for <paramref name="version"/> — and
    /// returns the inner wire schema with the null marker stripped, ready for
    /// a strict deep-compare against the wrapper's non-null wire form.
    ///
    /// Unlike <see cref="UnwrapNullableProperty"/>, which tolerates a missing
    /// marker (returning the schema unchanged), this fails when no marker is
    /// found — so it pins the bug fix: a nullable wrapper must keep its
    /// nullability instead of silently dropping it. Accepted shapes:
    /// <list type="bullet">
    ///   <item>3.0 flat (Swashbuckle): <c>{ …wire, "nullable": true }</c>.</item>
    ///   <item>3.0 union (Microsoft): <c>{ "oneOf": [ …wire ], "nullable": true }</c>.</item>
    ///   <item>3.1 union (Microsoft): <c>{ "oneOf": [ {"type":"null"}, …wire ] }</c>.</item>
    ///   <item>3.1 flat: <c>{ "type": ["X","null"], … }</c>.</item>
    /// </list>
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

using System.Text.Json;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Annotation-propagation assertions pin that bounds are reachable, not how the pipeline encoded them (inline merged
/// schema vs. layered <c>$ref</c> + <c>allOf</c>): the collectors walk every reachable layer and return the tightest
/// applicable bound, or <c>null</c> when no layer carries the keyword.
/// </summary>
internal static class SchemaWalk
{
    /// <summary>
    /// Skips only the null marker for <paramref name="version"/> — a cross-version marker is walked, so contamination
    /// surfaces downstream rather than silently passing through.
    /// </summary>
    internal static IEnumerable<JsonElement> WalkSchemaLayers(JsonElement doc, JsonElement schema, OpenApiVersion version)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<JsonElement>();
        queue.Enqueue(schema);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current.ValueKind != JsonValueKind.Object) continue;

            yield return current;

            if (current.TryGetProperty("$ref", out var refProp))
            {
                var path = refProp.GetString()!;
                const string prefix = "#/components/schemas/";
                if (path.StartsWith(prefix) && seen.Add(path))
                {
                    var name = path[prefix.Length..];
                    queue.Enqueue(doc.GetProperty("components").GetProperty("schemas").GetProperty(name));
                }
            }

            foreach (var key in new[] { "allOf", "oneOf", "anyOf" })
            {
                if (!current.TryGetProperty(key, out var arr) || arr.ValueKind != JsonValueKind.Array) continue;
                foreach (var branch in arr.EnumerateArray())
                {
                    if (branch.ValueKind == JsonValueKind.Object && NullableUnwrap.IsNullBranch(branch, version)) continue;
                    queue.Enqueue(branch);
                }
            }
        }
    }

    internal static int? CollectMaxInt(JsonElement doc, JsonElement schema, string keyword, OpenApiVersion version)
    {
        int? best = null;
        foreach (var layer in WalkSchemaLayers(doc, schema, version))
        {
            if (!layer.TryGetProperty(keyword, out var v) || v.ValueKind != JsonValueKind.Number) continue;
            var i = v.GetInt32();
            if (best is null || i > best) best = i;
        }
        return best;
    }

    internal static int? CollectMinInt(JsonElement doc, JsonElement schema, string keyword, OpenApiVersion version)
    {
        int? best = null;
        foreach (var layer in WalkSchemaLayers(doc, schema, version))
        {
            if (!layer.TryGetProperty(keyword, out var v) || v.ValueKind != JsonValueKind.Number) continue;
            var i = v.GetInt32();
            if (best is null || i < best) best = i;
        }
        return best;
    }

    internal static string? CollectFirstString(JsonElement doc, JsonElement schema, string keyword, OpenApiVersion version)
    {
        foreach (var layer in WalkSchemaLayers(doc, schema, version))
        {
            if (layer.TryGetProperty(keyword, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        }
        return null;
    }

    internal static decimal? CollectMaxLowerBound(JsonElement doc, JsonElement schema, OpenApiVersion version)
    {
        decimal? best = null;
        foreach (var layer in WalkSchemaLayers(doc, schema, version))
        {
            if (TryReadInclusiveLower(layer, out var v) && (best is null || v > best)) best = v;
        }
        return best;
    }

    internal static decimal? CollectMinUpperBound(JsonElement doc, JsonElement schema, OpenApiVersion version)
    {
        decimal? best = null;
        foreach (var layer in WalkSchemaLayers(doc, schema, version))
        {
            if (TryReadInclusiveUpper(layer, out var v) && (best is null || v < best)) best = v;
        }
        return best;
    }

    private static bool TryReadInclusiveLower(JsonElement layer, out decimal value)
    {
        value = 0m;
        if (layer.TryGetProperty("minimum", out var min) && min.ValueKind == JsonValueKind.Number)
        {
            value = min.GetDecimal();
            return true;
        }
        return false;
    }

    private static bool TryReadInclusiveUpper(JsonElement layer, out decimal value)
    {
        value = 0m;
        if (layer.TryGetProperty("maximum", out var max) && max.ValueKind == JsonValueKind.Number)
        {
            value = max.GetDecimal();
            return true;
        }
        return false;
    }
}

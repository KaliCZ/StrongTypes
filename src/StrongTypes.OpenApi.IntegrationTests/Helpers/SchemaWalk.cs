using System.Text.Json;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Walks a property schema across <c>$ref</c>, <c>allOf</c>,
/// <c>oneOf</c>, and <c>anyOf</c> layers (skipping the version-strict
/// null branches that encode <c>T?</c>) and pulls keyword values
/// reachable anywhere in the chain. Annotation-propagation
/// assertions don't care whether the pipeline inlined the merged
/// schema or layered the caller's bounds via <c>$ref</c> + <c>allOf</c>;
/// they care that the bounds are reachable.
/// </summary>
internal static class SchemaWalk
{
    /// <summary>
    /// Yields every schema layer reachable from <paramref name="schema"/>
    /// by following <c>$ref</c> and union/composition keywords.
    /// Skips the null marker for <paramref name="version"/>; a
    /// cross-version null marker is not skipped, so contamination
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

    /// <summary>
    /// Returns the largest integer value of <paramref name="keyword"/>
    /// reachable from <paramref name="schema"/>, or <c>null</c> if no
    /// layer carries the keyword. Useful for floors that can be
    /// tightened by an outer layer (e.g. <c>minLength</c>).
    /// </summary>
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

    /// <summary>
    /// Returns the smallest integer value of <paramref name="keyword"/>
    /// reachable from <paramref name="schema"/>, or <c>null</c> if no
    /// layer carries the keyword. Useful for ceilings that can be
    /// tightened by an outer layer (e.g. <c>maxLength</c>).
    /// </summary>
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

    /// <summary>
    /// Returns the first string value of <paramref name="keyword"/>
    /// reachable from <paramref name="schema"/>, or <c>null</c> if no
    /// layer carries the keyword. For string keywords like
    /// <c>format</c>, <c>pattern</c>, and <c>description</c> where
    /// any reachable value is the value the caller observes.
    /// </summary>
    internal static string? CollectFirstString(JsonElement doc, JsonElement schema, string keyword, OpenApiVersion version)
    {
        foreach (var layer in WalkSchemaLayers(doc, schema, version))
        {
            if (layer.TryGetProperty(keyword, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        }
        return null;
    }

    /// <summary>
    /// Returns the largest inclusive-lower-bound (<c>minimum</c>)
    /// reachable from <paramref name="schema"/>. The tightest applicable
    /// floor wins, modelling how a layered caller bound stacks on top
    /// of a wrapper's own floor.
    /// </summary>
    internal static decimal? CollectMaxLowerBound(JsonElement doc, JsonElement schema, OpenApiVersion version)
    {
        decimal? best = null;
        foreach (var layer in WalkSchemaLayers(doc, schema, version))
        {
            if (TryReadInclusiveLower(layer, out var v) && (best is null || v > best)) best = v;
        }
        return best;
    }

    /// <summary>
    /// Returns the smallest inclusive-upper-bound (<c>maximum</c>)
    /// reachable from <paramref name="schema"/>. The tightest applicable
    /// ceiling wins.
    /// </summary>
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

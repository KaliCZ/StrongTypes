using System.Text.Json;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

internal static class ComponentSchemas
{
    /// <summary>Returns the names of every schema in <c>components.schemas</c>, or an empty array when the section is absent.</summary>
    internal static string[] ReadComponentSchemaNames(JsonElement doc)
    {
        if (!doc.TryGetProperty("components", out var components)) return [];
        if (!components.TryGetProperty("schemas", out var schemas)) return [];
        return schemas.EnumerateObject().Select(p => p.Name).ToArray();
    }

    /// <summary>Returns the set of <c>components.schemas</c> names that some <c>$ref</c> in the document points at.</summary>
    internal static HashSet<string> ReadReferencedSchemaNames(JsonElement doc)
    {
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        CollectRefs(doc, referenced);
        return referenced;
    }

    private static void CollectRefs(JsonElement element, HashSet<string> referenced)
    {
        const string prefix = "#/components/schemas/";
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.NameEquals("$ref") && prop.Value.ValueKind == JsonValueKind.String
                        && prop.Value.GetString() is { } path && path.StartsWith(prefix, StringComparison.Ordinal))
                        referenced.Add(path[prefix.Length..]);
                    else
                        CollectRefs(prop.Value, referenced);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectRefs(item, referenced);
                break;
        }
    }

    /// <summary>
    /// Wrapper generics are named per pipeline: Microsoft's prefix style (<c>PositiveOf…</c>), Swashbuckle's suffix
    /// style (<c>…Positive</c>).
    /// </summary>
    internal static bool IsInlineableWrapperName(string name)
    {
        if (string.Equals(name, "NonEmptyString", StringComparison.Ordinal)) return true;
        if (string.Equals(name, "Email", StringComparison.Ordinal)) return true;
        if (string.Equals(name, "Digit", StringComparison.Ordinal)) return true;

        if (name.StartsWith("PositiveOf", StringComparison.Ordinal)
            || name.StartsWith("NonNegativeOf", StringComparison.Ordinal)
            || name.StartsWith("NegativeOf", StringComparison.Ordinal)
            || name.StartsWith("NonPositiveOf", StringComparison.Ordinal)
            || name.StartsWith("NonEmptyEnumerableOf", StringComparison.Ordinal)
            || name.StartsWith("MaybeOf", StringComparison.Ordinal))
            return true;

        return name.EndsWith("Positive", StringComparison.Ordinal)
            || name.EndsWith("Negative", StringComparison.Ordinal)
            || name.EndsWith("NonNegative", StringComparison.Ordinal)
            || name.EndsWith("NonPositive", StringComparison.Ordinal)
            || name.EndsWith("NonEmptyEnumerable", StringComparison.Ordinal)
            || name.EndsWith("Maybe", StringComparison.Ordinal);
    }
}

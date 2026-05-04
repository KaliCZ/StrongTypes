using System.Text.Json;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Operations on the <c>components.schemas</c> section of an OpenAPI
/// document — used to pin which wrapper components survive the
/// inliner pass and which are expected to be removed.
/// </summary>
internal static class ComponentSchemas
{
    /// <summary>
    /// Returns the names of every schema in <c>components.schemas</c>,
    /// or an empty array when the section is absent.
    /// </summary>
    internal static string[] ReadComponentSchemaNames(JsonElement doc)
    {
        if (!doc.TryGetProperty("components", out var components)) return [];
        if (!components.TryGetProperty("schemas", out var schemas)) return [];
        return schemas.EnumerateObject().Select(p => p.Name).ToArray();
    }

    /// <summary>
    /// Identifies a component schema name as one of the wrapper types
    /// the inliner is expected to remove. Recognises both the Microsoft
    /// prefix style (<c>PositiveOf…</c>, <c>MaybeOf…</c>) and the
    /// Swashbuckle suffix style (<c>…Positive</c>, <c>…Maybe</c>).
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

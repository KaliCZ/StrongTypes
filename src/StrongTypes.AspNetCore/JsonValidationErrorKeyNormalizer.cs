using System;

namespace StrongTypes.AspNetCore;

/// <summary>
/// Rewrites a System.Text.Json error path (e.g. <c>$.value</c>,
/// <c>$.items[0].name</c>) into the model-binding key form (<c>Value</c>,
/// <c>Items[0].Name</c>) MVC uses for model-binding and data-annotation errors.
/// </summary>
/// <remarks>
/// There is no metadata at this layer, so a custom <c>[JsonPropertyName]</c> that is not just a
/// re-cased property name cannot be recovered.
/// </remarks>
internal static class JsonValidationErrorKeyNormalizer
{
    /// <summary>
    /// Keys that do not start with the JSON root token (<c>$</c>) come from model binding, not
    /// the body, and pass through unchanged.
    /// </summary>
    public static string Normalize(string key, JsonErrorKeyCasing casing)
    {
        if (key.Length == 0 || key[0] != '$') return key;

        var path = key[1..];
        if (path.StartsWith('.')) path = path[1..];
        if (path.Length == 0 || casing == JsonErrorKeyCasing.StripOnly) return path;

        var segments = path.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = ApplyCasing(segments[i], casing);
        }

        return string.Join('.', segments);
    }

    private static string ApplyCasing(string segment, JsonErrorKeyCasing casing)
    {
        var bracket = segment.IndexOf('[');
        var name = bracket < 0 ? segment : segment[..bracket];
        if (name.Length == 0) return segment;

        var first = casing == JsonErrorKeyCasing.PascalCase
            ? char.ToUpperInvariant(name[0])
            : char.ToLowerInvariant(name[0]);
        var suffix = bracket < 0 ? string.Empty : segment[bracket..];
        return $"{first}{name[1..]}{suffix}";
    }
}

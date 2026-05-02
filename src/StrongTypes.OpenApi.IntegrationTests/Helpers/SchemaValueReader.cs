using System.Text.Json;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Typed value extraction from JSON-schema keywords. Each reader returns
/// <c>null</c> (or <c>false</c> for booleans) when the keyword is absent
/// or has an unexpected JSON kind, so call sites can use the result
/// directly in assertions without a precondition check.
/// </summary>
internal static class SchemaValueReader
{
    internal static string? StringOrNull(JsonElement schema, string propertyName) =>
        schema.TryGetProperty(propertyName, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    internal static int? IntOrNull(JsonElement schema, string propertyName) =>
        schema.TryGetProperty(propertyName, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetInt32()
            : null;

    internal static decimal? DecimalOrNull(JsonElement schema, string propertyName) =>
        schema.TryGetProperty(propertyName, out var v) && v.ValueKind == JsonValueKind.Number
            ? v.GetDecimal()
            : null;

    internal static bool BoolOrFalse(JsonElement schema, string propertyName) =>
        schema.TryGetProperty(propertyName, out var v) && v.ValueKind == JsonValueKind.True;

    /// <summary>
    /// Returns the names listed in the schema's <c>required</c> array,
    /// or an empty array when the keyword is absent or not an array.
    /// </summary>
    internal static string[] ReadRequiredArray(JsonElement schema)
    {
        if (!schema.TryGetProperty("required", out var req) || req.ValueKind != JsonValueKind.Array)
            return [];
        return req.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToArray();
    }
}

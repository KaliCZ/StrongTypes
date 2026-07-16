using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Core;

/// <summary>Schema-keyword helpers that write a wrapper's wire form without overwriting stricter caller-supplied values.</summary>
public static class SchemaPaint
{
    /// <summary>Strips the CLR-wrapper-derived structural shape, leaving caller-set bounds and annotations alone.</summary>
    public static void ClearWrapperShape(OpenApiSchema schema)
    {
        schema.Properties?.Clear();
        schema.Required?.Clear();
        schema.AllOf?.Clear();
        schema.OneOf?.Clear();
        schema.AnyOf?.Clear();
        schema.AdditionalProperties = null;
        schema.AdditionalPropertiesAllowed = true;
        schema.Items = null;
    }

    public static bool IsNullable(OpenApiSchema schema)
        => schema.Type is { } type && type.HasFlag(JsonSchemaType.Null);

    public static void MarkNullable(OpenApiSchema schema)
        => schema.Type = (schema.Type ?? JsonSchemaType.Null) | JsonSchemaType.Null;

    /// <summary>Raises <c>minLength</c> to <paramref name="floor"/>, keeping the caller's value when already at least as large.</summary>
    public static void TightenMinLength(OpenApiSchema schema, int floor)
    {
        if (schema.MinLength is { } current && current >= floor) return;
        schema.MinLength = floor;
    }

    /// <summary>Raises <c>minItems</c> to <paramref name="floor"/>, keeping the caller's value when already at least as large.</summary>
    public static void TightenMinItems(OpenApiSchema schema, int floor)
    {
        if (schema.MinItems is { } current && current >= floor) return;
        schema.MinItems = floor;
    }

    /// <summary>Lowers <c>maxLength</c> to <paramref name="ceiling"/>, keeping the caller's value when already at least as small.</summary>
    public static void TightenMaxLength(OpenApiSchema schema, int ceiling)
    {
        if (schema.MaxLength is { } current && current <= ceiling) return;
        schema.MaxLength = ceiling;
    }

    /// <summary>Lowers <c>maxItems</c> to <paramref name="ceiling"/>, keeping the caller's value when already at least as small.</summary>
    public static void TightenMaxItems(OpenApiSchema schema, int ceiling)
    {
        if (schema.MaxItems is { } current && current <= ceiling) return;
        schema.MaxItems = ceiling;
    }

    public static void SetPatternIfAbsent(OpenApiSchema schema, string pattern)
    {
        if (!string.IsNullOrEmpty(schema.Pattern)) return;
        schema.Pattern = pattern;
    }

    public static void SetFormatIfAbsent(OpenApiSchema schema, string format)
    {
        if (!string.IsNullOrEmpty(schema.Format)) return;
        schema.Format = format;
    }

    public static void SetDescriptionIfAbsent(OpenApiSchema schema, string description)
    {
        if (!string.IsNullOrEmpty(schema.Description)) return;
        schema.Description = description;
    }

    public static void SetDefaultIfAbsent(OpenApiSchema schema, JsonNode @default)
    {
        if (schema.Default is not null) return;
        schema.Default = @default;
    }

    /// <summary>Raises the schema's lower bound to <paramref name="floorValue"/> (exclusive when <paramref name="floorExclusive"/> is <c>true</c>), keeping the caller's bound when already at least as tight.</summary>
    public static void TightenLowerBound(OpenApiSchema schema, decimal floorValue, bool floorExclusive)
    {
        if (ReadEffectiveLowerBound(schema) is { } caller && IsLowerAtLeastAsTight(caller, (floorValue, floorExclusive)))
            return;

        schema.Minimum = null;
        schema.ExclusiveMinimum = null;
        var text = floorValue.ToString(CultureInfo.InvariantCulture);
        if (floorExclusive) schema.ExclusiveMinimum = text;
        else schema.Minimum = text;
    }

    /// <summary>Lowers the schema's upper bound to <paramref name="floorValue"/> (exclusive when <paramref name="floorExclusive"/> is <c>true</c>), keeping the caller's bound when already at least as tight.</summary>
    public static void TightenUpperBound(OpenApiSchema schema, decimal floorValue, bool floorExclusive)
    {
        if (ReadEffectiveUpperBound(schema) is { } caller && IsUpperAtLeastAsTight(caller, (floorValue, floorExclusive)))
            return;

        schema.Maximum = null;
        schema.ExclusiveMaximum = null;
        var text = floorValue.ToString(CultureInfo.InvariantCulture);
        if (floorExclusive) schema.ExclusiveMaximum = text;
        else schema.Maximum = text;
    }

    private static (decimal Value, bool Exclusive)? ReadEffectiveLowerBound(OpenApiSchema schema)
    {
        var inc = TryParseDecimal(schema.Minimum);
        var exc = TryParseDecimal(schema.ExclusiveMinimum);
        if (inc is null && exc is null) return null;
        if (exc is null) return (inc!.Value, false);
        if (inc is null) return (exc.Value, true);
        return inc.Value > exc.Value ? (inc.Value, false) : (exc.Value, true);
    }

    private static (decimal Value, bool Exclusive)? ReadEffectiveUpperBound(OpenApiSchema schema)
    {
        var inc = TryParseDecimal(schema.Maximum);
        var exc = TryParseDecimal(schema.ExclusiveMaximum);
        if (inc is null && exc is null) return null;
        if (exc is null) return (inc!.Value, false);
        if (inc is null) return (exc.Value, true);
        return inc.Value < exc.Value ? (inc.Value, false) : (exc.Value, true);
    }

    private static decimal? TryParseDecimal(string? value) =>
        value is not null && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
            ? d
            : null;

    private static bool IsLowerAtLeastAsTight((decimal Value, bool Exclusive) caller, (decimal Value, bool Exclusive) floor)
    {
        if (caller.Value > floor.Value) return true;
        if (caller.Value < floor.Value) return false;
        return caller.Exclusive || !floor.Exclusive;
    }

    private static bool IsUpperAtLeastAsTight((decimal Value, bool Exclusive) caller, (decimal Value, bool Exclusive) floor)
    {
        if (caller.Value < floor.Value) return true;
        if (caller.Value > floor.Value) return false;
        return caller.Exclusive || !floor.Exclusive;
    }
}

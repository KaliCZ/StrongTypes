using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace StrongTypes.OpenApi.Core;

/// <summary>
/// Painter primitives shared by the Microsoft and Swashbuckle adapters. The
/// design point: a strong-type wrapper <em>is</em> its underlying primitive on
/// the wire, so the spec generator's reflected wrapper shape (an object with
/// <c>Value</c> / <c>Length</c> / <c>IsSome</c> sub-properties) is noise and
/// must be removed. Anything <em>else</em> the generator or the caller put on
/// the schema — <c>[StringLength]</c>, <c>[RegularExpression]</c>,
/// <c>[Range]</c>, custom descriptions, examples, third-party schema filters —
/// is the caller's contract and must be preserved.
/// </summary>
public static class SchemaPaint
{
    /// <summary>
    /// Strips only the CLR-wrapper-derived shape so the painter can write the
    /// wire primitive over the top. Caller-set bounds (<c>MinLength</c>,
    /// <c>Pattern</c>, <c>Minimum</c>, …), <c>Description</c>, <c>Example</c>,
    /// <c>Enum</c>, <c>Default</c>, and any other annotations are left alone —
    /// they survive the paint and stack with whatever floor the painter applies
    /// next.
    /// </summary>
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

    /// <summary>
    /// Applies a <c>minLength</c> floor, keeping the caller's value when it is
    /// already at least as tight (e.g. caller's <c>minLength: 3</c> beats a
    /// floor of <c>1</c>).
    /// </summary>
    public static void TightenMinLength(OpenApiSchema schema, int floor)
    {
        if (schema.MinLength is { } current && current >= floor) return;
        schema.MinLength = floor;
    }

    /// <summary>
    /// Applies a <c>minItems</c> floor, keeping the caller's value when it is
    /// already at least as tight.
    /// </summary>
    public static void TightenMinItems(OpenApiSchema schema, int floor)
    {
        if (schema.MinItems is { } current && current >= floor) return;
        schema.MinItems = floor;
    }

    /// <summary>
    /// Applies a <c>maxLength</c> ceiling, keeping the caller's value when it
    /// is already at least as tight (i.e. smaller).
    /// </summary>
    public static void TightenMaxLength(OpenApiSchema schema, int ceiling)
    {
        if (schema.MaxLength is { } current && current <= ceiling) return;
        schema.MaxLength = ceiling;
    }

    /// <summary>
    /// Applies a <c>maxItems</c> ceiling, keeping the caller's value when it
    /// is already at least as tight.
    /// </summary>
    public static void TightenMaxItems(OpenApiSchema schema, int ceiling)
    {
        if (schema.MaxItems is { } current && current <= ceiling) return;
        schema.MaxItems = ceiling;
    }

    /// <summary>
    /// Sets <c>pattern</c> only when the schema doesn't already carry one.
    /// </summary>
    public static void SetPatternIfAbsent(OpenApiSchema schema, string pattern)
    {
        if (!string.IsNullOrEmpty(schema.Pattern)) return;
        schema.Pattern = pattern;
    }

    /// <summary>
    /// Sets <c>format</c> only when the schema doesn't already carry one.
    /// </summary>
    public static void SetFormatIfAbsent(OpenApiSchema schema, string format)
    {
        if (!string.IsNullOrEmpty(schema.Format)) return;
        schema.Format = format;
    }

    /// <summary>
    /// Sets <c>description</c> only when the schema doesn't already carry one.
    /// </summary>
    public static void SetDescriptionIfAbsent(OpenApiSchema schema, string description)
    {
        if (!string.IsNullOrEmpty(schema.Description)) return;
        schema.Description = description;
    }

    /// <summary>
    /// Sets <c>default</c> only when the schema doesn't already carry one.
    /// </summary>
    public static void SetDefaultIfAbsent(OpenApiSchema schema, JsonNode @default)
    {
        if (schema.Default is not null) return;
        schema.Default = @default;
    }

    /// <summary>
    /// Applies a numeric lower-bound floor (inclusive when
    /// <paramref name="floorExclusive"/> is <c>false</c>, exclusive otherwise).
    /// If the caller's effective lower bound is already at least as tight as
    /// the floor, both fields are left as-is. Otherwise the caller's looser
    /// fields are cleared and the floor is written.
    /// </summary>
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

    /// <summary>
    /// Applies a numeric upper-bound floor (inclusive when
    /// <paramref name="floorExclusive"/> is <c>false</c>, exclusive otherwise).
    /// If the caller's effective upper bound is already at least as tight as
    /// the floor, both fields are left as-is.
    /// </summary>
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

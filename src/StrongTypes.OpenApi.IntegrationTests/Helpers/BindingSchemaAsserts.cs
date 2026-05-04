using System.Text.Json;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Schema-shape assertions for parameters bound from non-body sources
/// (<c>[FromQuery]</c>, <c>[FromRoute]</c>, <c>[FromHeader]</c>) and for
/// individual properties of <c>[FromForm]</c> request bodies. The same
/// assertion runs on every pipeline; when a pipeline gets the schema
/// wrong it sets the matching "broken" flag and the helper asserts the
/// <em>actual</em> broken shape (rather than silently skipping). The day
/// the pipeline starts emitting the correct shape, the broken-path assert
/// fails and the flag must be flipped to <c>false</c> in the subclass.
///
/// Every shape helper deep-compares the schema against a literal JSON
/// snapshot via <see cref="AssertJsonEquals"/>, so the helper body reads
/// as the exact expected schema and any unexpected keyword fails the test.
///
/// Per-source splits exist where the broken shape genuinely differs by
/// binding source — most notably <see cref="AssertRoutePositiveInt"/>,
/// because a route <c>:int</c> constraint preserves <c>{type:integer,
/// format:int32}</c> while every other source falls all the way back to
/// <c>{type:string}</c>. Where the broken shape is consistent across
/// sources (NonEmptyString, Email) a single helper covers all of them.
///
/// Each <c>isXBroken</c> parameter mirrors the name of the flag on
/// <see cref="OpenApiDocumentTestsBase"/> that it carries — so a call site
/// reads as <c>AssertNonBodyEmail(schema, IsNonJsonBodyStrongTypeSchemaBroken,
/// IsEmailStringFormatBroken)</c> with no ambiguity about which flag is which.
/// </summary>
internal static class BindingSchemaAsserts
{
    /// <summary>
    /// Number of fields in <c>BindingProbeFormRequest</c>. The Swashbuckle
    /// broken-path navigates the form schema's <c>allOf</c> by declaration
    /// index and asserts this count to lock in that the broken shape is the
    /// one we expect.
    /// </summary>
    private const int FormFieldCount = 6;

    // ── NonEmptyString ───────────────────────────────────────────────────

    internal static void AssertNonBodyNonEmptyString(JsonElement schema, bool isNonJsonBodyStrongTypeSchemaBroken)
    {
        if (isNonJsonBodyStrongTypeSchemaBroken) AssertBrokenNonEmptyString(schema);
        else AssertCorrectNonEmptyString(schema);
    }

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyNonEmptyString(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken, bool isNonJsonBodyStrongTypeSchemaBroken)
    {
        var schema = GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken);
        if (isNonJsonBodyStrongTypeSchemaBroken) AssertBrokenNonEmptyString(schema);
        else AssertCorrectNonEmptyString(schema);
    }

    private static void AssertCorrectNonEmptyString(JsonElement schema)
        => AssertJsonEquals(schema, """{"type":"string","minLength":1}""");

    private static void AssertBrokenNonEmptyString(JsonElement schema)
        // Schema transformer didn't run; the underlying string type survives
        // but the NonEmptyString-specific minLength: 1 is missing.
        => AssertJsonEquals(schema, """{"type":"string"}""");

    // ── Positive<int> ────────────────────────────────────────────────────
    // Broken shape splits by source: a route :int constraint preserves
    // {type:integer, format:int32}; every other source falls back to
    // {type:string} because there's no source-side type metadata.
    // Correct shape splits by OpenAPI version: 3.0 encodes the exclusive
    // bound as {minimum:0, exclusiveMinimum:true} (boolean pair); 3.1 as
    // {exclusiveMinimum:0} (numeric).

    /// <summary>
    /// Asserts the schema for a <c>[FromQuery]</c> or <c>[FromHeader]</c>
    /// <c>Positive&lt;int&gt;</c> parameter. On the broken path the underlying
    /// type falls back to <c>{type:string}</c>.
    /// </summary>
    internal static void AssertNonBodyPositiveInt(JsonElement schema, bool isNonJsonBodyStrongTypeSchemaBroken, OpenApiVersion version)
    {
        if (isNonJsonBodyStrongTypeSchemaBroken) AssertBrokenPositiveIntStringFallback(schema);
        else AssertCorrectPositiveInt(schema, version);
    }

    /// <summary>
    /// Asserts the schema for a <c>[FromRoute]</c> <c>Positive&lt;int&gt;</c>
    /// parameter. On the broken path the route <c>:int</c> constraint
    /// preserves <c>{type:integer, format:int32}</c>.
    /// </summary>
    internal static void AssertRoutePositiveInt(JsonElement schema, bool isNonJsonBodyStrongTypeSchemaBroken, OpenApiVersion version)
    {
        if (isNonJsonBodyStrongTypeSchemaBroken) AssertBrokenPositiveIntIntegerWithFormat(schema);
        else AssertCorrectPositiveInt(schema, version);
    }

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyPositiveInt(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken, bool isNonJsonBodyStrongTypeSchemaBroken, OpenApiVersion version)
    {
        var schema = GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken);
        if (isNonJsonBodyStrongTypeSchemaBroken) AssertBrokenPositiveIntStringFallback(schema);
        else AssertCorrectPositiveInt(schema, version);
    }

    private static void AssertCorrectPositiveInt(JsonElement schema, OpenApiVersion version)
        => AssertJsonEquals(schema, version switch
        {
            OpenApiVersion.V3_0 => """{"type":"integer","format":"int32","minimum":0,"exclusiveMinimum":true}""",
            OpenApiVersion.V3_1 => """{"type":"integer","format":"int32","exclusiveMinimum":0}""",
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null),
        });

    private static void AssertBrokenPositiveIntStringFallback(JsonElement schema)
        // Schema transformer didn't run and there's no source-side metadata
        // (no route :int constraint, etc.); the type falls all the way back
        // to a string with neither format nor exclusiveMinimum.
        => AssertJsonEquals(schema, """{"type":"string"}""");

    private static void AssertBrokenPositiveIntIntegerWithFormat(JsonElement schema)
        // Schema transformer didn't run, but the route :int constraint
        // preserves the underlying integer type. exclusiveMinimum is absent.
        => AssertJsonEquals(schema, """{"type":"integer","format":"int32"}""");

    // ── Email ────────────────────────────────────────────────────────────

    internal static void AssertNonBodyEmail(JsonElement schema, bool isNonJsonBodyStrongTypeSchemaBroken, bool isEmailStringFormatBroken)
    {
        if (isNonJsonBodyStrongTypeSchemaBroken) AssertBrokenEmail(schema);
        else AssertCorrectEmail(schema, isEmailStringFormatBroken);
    }

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyEmail(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken, bool isNonJsonBodyStrongTypeSchemaBroken, bool isEmailStringFormatBroken)
    {
        var schema = GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken);
        if (isNonJsonBodyStrongTypeSchemaBroken) AssertBrokenEmail(schema);
        else AssertCorrectEmail(schema, isEmailStringFormatBroken);
    }

    private static void AssertCorrectEmail(JsonElement schema, bool isEmailStringFormatBroken)
        => AssertJsonEquals(schema, isEmailStringFormatBroken
            ? """{"type":"string","minLength":1,"maxLength":254}"""
            : """{"type":"string","minLength":1,"maxLength":254,"format":"email"}""");

    private static void AssertBrokenEmail(JsonElement schema)
        // Schema transformer didn't run; the underlying string type survives
        // but none of the Email-specific keywords appear.
        => AssertJsonEquals(schema, """{"type":"string"}""");

    /// <summary>
    /// Looks up a per-field schema on a <c>[FromForm]</c> request-body
    /// schema. On the not-broken path the form schema is an object with a
    /// <c>properties</c> map; the field is found by name (case-insensitive
    /// because Microsoft emits PascalCase, Swashbuckle camelCase). On the
    /// broken path the form schema is <c>{ allOf: [&lt;each-field&gt;] }</c>
    /// with names dropped; the field is found by its declaration index
    /// (<paramref name="allOfIndex"/>). Either way, the per-field schema
    /// returned is then asserted with the same strong-type assertion.
    /// </summary>
    private static JsonElement GetFormProperty(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken)
    {
        if (isFormPropertiesSchemaBroken)
        {
            Assert.True(formSchema.TryGetProperty("allOf", out var allOf), "form schema is broken-flagged but has no allOf");
            Assert.Equal(JsonValueKind.Array, allOf.ValueKind);
            Assert.Equal(FormFieldCount, allOf.GetArrayLength());
            Assert.False(formSchema.TryGetProperty("properties", out _), "form schema is broken-flagged but still has a properties map");
            return allOf[allOfIndex];
        }

        var properties = formSchema.GetProperty("properties");
        foreach (var entry in properties.EnumerateObject())
        {
            if (string.Equals(entry.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                return entry.Value;
        }
        Assert.Fail($"form schema has no '{propertyName}' property (case-insensitive)");
        return default;
    }
}

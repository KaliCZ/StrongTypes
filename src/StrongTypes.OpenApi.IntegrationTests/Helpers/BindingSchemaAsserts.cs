using System.Text.Json;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ExclusiveBounds;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;

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
    // Broken shape is consistent across every source (always falls back to
    // {type:string} with minLength missing), so one helper covers all of them.

    internal static void AssertNonBodyNonEmptyString(JsonElement schema, bool isNonJsonBodyStrongTypeSchemaBroken)
        => AssertNonEmptyStringSchema(schema, isNonJsonBodyStrongTypeSchemaBroken, assertInline: true);

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyNonEmptyString(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken, bool isNonJsonBodyStrongTypeSchemaBroken)
        => AssertNonEmptyStringSchema(GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken), isNonJsonBodyStrongTypeSchemaBroken, assertInline: false);

    private static void AssertNonEmptyStringSchema(JsonElement schema, bool isNonJsonBodyStrongTypeSchemaBroken, bool assertInline)
    {
        if (assertInline) AssertInlineSchema(schema);
        Assert.Equal("string", StringOrNull(schema, "type"));
        if (isNonJsonBodyStrongTypeSchemaBroken)
        {
            // Schema transformer didn't run; the NonEmptyString-specific
            // minLength: 1 is missing. The day it starts running, this assert
            // fails and the broken flag must be flipped.
            Assert.Null(IntOrNull(schema, "minLength"));
            return;
        }
        Assert.Equal(1, IntOrNull(schema, "minLength"));
    }

    // ── Positive<int> ────────────────────────────────────────────────────
    // Broken shape splits by source: a route :int constraint preserves
    // {type:integer, format:int32}; every other source falls back to
    // {type:string} because there's no source-side type metadata.

    /// <summary>
    /// Asserts the schema for a <c>[FromQuery]</c> or <c>[FromHeader]</c>
    /// <c>Positive&lt;int&gt;</c> parameter. On the broken path the underlying
    /// type falls back to <c>{type:string}</c> with no <c>format</c> and no
    /// <c>exclusiveMinimum</c>.
    /// </summary>
    internal static void AssertNonBodyPositiveInt(JsonElement schema, bool isNonJsonBodyStrongTypeSchemaBroken, OpenApiVersion version)
    {
        AssertInlineSchema(schema);
        if (isNonJsonBodyStrongTypeSchemaBroken)
        {
            AssertBrokenPositiveIntStringFallback(schema);
            return;
        }
        AssertPositiveIntCorrect(schema, version);
    }

    /// <summary>
    /// Asserts the schema for a <c>[FromRoute]</c> <c>Positive&lt;int&gt;</c>
    /// parameter. On the broken path the route <c>:int</c> constraint
    /// preserves the underlying type as <c>{type:integer, format:int32}</c>,
    /// but no <c>exclusiveMinimum</c> is emitted.
    /// </summary>
    internal static void AssertRoutePositiveInt(JsonElement schema, bool isNonJsonBodyStrongTypeSchemaBroken, OpenApiVersion version)
    {
        AssertInlineSchema(schema);
        if (isNonJsonBodyStrongTypeSchemaBroken)
        {
            AssertBrokenPositiveIntIntegerWithFormat(schema);
            return;
        }
        AssertPositiveIntCorrect(schema, version);
    }

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyPositiveInt(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken, bool isNonJsonBodyStrongTypeSchemaBroken, OpenApiVersion version)
    {
        var schema = GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken);
        if (isNonJsonBodyStrongTypeSchemaBroken)
        {
            AssertBrokenPositiveIntStringFallback(schema);
            return;
        }
        AssertPositiveIntCorrect(schema, version);
    }

    private static void AssertPositiveIntCorrect(JsonElement schema, OpenApiVersion version)
    {
        Assert.Equal("integer", StringOrNull(schema, "type"));
        Assert.Equal("int32", StringOrNull(schema, "format"));
        AssertExclusiveLowerBound(schema, 0m, version);
    }

    private static void AssertBrokenPositiveIntStringFallback(JsonElement schema)
    {
        // Schema transformer didn't run and there's no source-side metadata
        // (no route :int constraint, etc.); the type falls all the way back
        // to a string. The day the pipeline starts honouring the strong-type
        // transformer (or even just preserving the underlying integer type),
        // this assert fails and the flag must be flipped.
        Assert.Equal("string", StringOrNull(schema, "type"));
        Assert.Null(StringOrNull(schema, "format"));
        Assert.False(schema.TryGetProperty("exclusiveMinimum", out _), "exclusiveMinimum should not be present on broken schema");
    }

    private static void AssertBrokenPositiveIntIntegerWithFormat(JsonElement schema)
    {
        // Schema transformer didn't run, but the route :int constraint
        // preserves the underlying integer type. No exclusiveMinimum is
        // emitted. The day the transformer starts running on route schemas,
        // exclusiveMinimum appears, this assert fails, and the flag must be
        // flipped.
        Assert.Equal("integer", StringOrNull(schema, "type"));
        Assert.Equal("int32", StringOrNull(schema, "format"));
        Assert.False(schema.TryGetProperty("exclusiveMinimum", out _), "exclusiveMinimum should not be present on broken schema");
    }

    // ── Email ────────────────────────────────────────────────────────────
    // Broken shape is consistent across every source (always {type:string}
    // with the Email-specific keywords absent), so one helper covers all.

    internal static void AssertNonBodyEmail(JsonElement schema, bool isNonJsonBodyStrongTypeSchemaBroken, bool isEmailStringFormatBroken)
        => AssertEmailSchema(schema, isNonJsonBodyStrongTypeSchemaBroken, isEmailStringFormatBroken, assertInline: true);

    /// <param name="propertyName">Field name as it appears in the form schema's <c>properties</c> map on the not-broken path.</param>
    /// <param name="allOfIndex">Position in the form schema's <c>allOf</c> array, used only when <paramref name="isFormPropertiesSchemaBroken"/> is true (the field name has been dropped, so navigation is by declaration index).</param>
    internal static void AssertFormPropertyEmail(JsonElement formSchema, string propertyName, int allOfIndex, bool isFormPropertiesSchemaBroken, bool isNonJsonBodyStrongTypeSchemaBroken, bool isEmailStringFormatBroken)
        => AssertEmailSchema(GetFormProperty(formSchema, propertyName, allOfIndex, isFormPropertiesSchemaBroken), isNonJsonBodyStrongTypeSchemaBroken, isEmailStringFormatBroken, assertInline: false);

    private static void AssertEmailSchema(JsonElement schema, bool isNonJsonBodyStrongTypeSchemaBroken, bool isEmailStringFormatBroken, bool assertInline)
    {
        if (assertInline) AssertInlineSchema(schema);
        Assert.Equal("string", StringOrNull(schema, "type"));
        if (isNonJsonBodyStrongTypeSchemaBroken)
        {
            // Schema transformer didn't run; none of the Email-specific
            // keywords appear (no minLength, maxLength, or format).
            Assert.Null(IntOrNull(schema, "minLength"));
            Assert.Null(IntOrNull(schema, "maxLength"));
            Assert.Null(StringOrNull(schema, "format"));
            return;
        }
        Assert.Equal(1, IntOrNull(schema, "minLength"));
        Assert.Equal(254, IntOrNull(schema, "maxLength"));
        if (!isEmailStringFormatBroken)
            Assert.Equal("email", StringOrNull(schema, "format"));
    }

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

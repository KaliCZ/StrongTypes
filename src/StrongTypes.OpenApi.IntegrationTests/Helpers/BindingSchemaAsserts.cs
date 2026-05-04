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
/// wrong it sets the matching "broken" flag and the helper asserts that
/// the strong-type-specific keywords are <em>absent</em> (rather than
/// silently skipping). The day the pipeline starts honouring the schema
/// transformer those keywords appear, the broken-path assertion fails,
/// and the flag must be flipped to <c>false</c> in the subclass.
/// </summary>
internal static class BindingSchemaAsserts
{
    internal static void AssertNonBodyNonEmptyString(JsonElement schema, bool isBroken)
        => AssertNonEmptyStringSchema(schema, isBroken, assertInline: true);

    internal static void AssertNonBodyPositiveInt(JsonElement schema, bool isBroken, OpenApiVersion version)
        => AssertPositiveIntSchema(schema, isBroken, version, assertInline: true);

    internal static void AssertNonBodyEmail(JsonElement schema, bool isBroken, bool isEmailFormatBroken)
        => AssertEmailSchema(schema, isBroken, isEmailFormatBroken, assertInline: true);

    internal static void AssertFormPropertyNonEmptyString(JsonElement formSchema, string propertyName, bool isStrongTypeBroken)
        => AssertNonEmptyStringSchema(GetFormProperty(formSchema, propertyName), isStrongTypeBroken, assertInline: false);

    internal static void AssertFormPropertyPositiveInt(JsonElement formSchema, string propertyName, bool isStrongTypeBroken, OpenApiVersion version)
        => AssertPositiveIntSchema(GetFormProperty(formSchema, propertyName), isStrongTypeBroken, version, assertInline: false);

    internal static void AssertFormPropertyEmail(JsonElement formSchema, string propertyName, bool isStrongTypeBroken, bool isEmailFormatBroken)
        => AssertEmailSchema(GetFormProperty(formSchema, propertyName), isStrongTypeBroken, isEmailFormatBroken, assertInline: false);

    private static void AssertNonEmptyStringSchema(JsonElement schema, bool isBroken, bool assertInline)
    {
        if (assertInline) AssertInlineSchema(schema);
        Assert.Equal("string", StringOrNull(schema, "type"));
        if (isBroken)
        {
            // Schema transformer didn't run; the NonEmptyString-specific
            // minLength: 1 is missing. The day it starts running, this assert
            // fails and the broken flag must be flipped.
            Assert.Null(IntOrNull(schema, "minLength"));
            return;
        }
        Assert.Equal(1, IntOrNull(schema, "minLength"));
    }

    private static void AssertPositiveIntSchema(JsonElement schema, bool isBroken, OpenApiVersion version, bool assertInline)
    {
        if (assertInline) AssertInlineSchema(schema);
        if (isBroken)
        {
            // Schema transformer didn't run. The underlying type may or may
            // not survive: with a route :int constraint Microsoft emits
            // {type:integer, format:int32}; without one it falls all the way
            // back to {type:string}. In neither case is exclusiveMinimum
            // emitted. The day either the type is wrong-but-fixable
            // (integer + exclusiveMinimum appears) or fully fixed, this
            // assert fails and the flag must be flipped.
            Assert.False(schema.TryGetProperty("exclusiveMinimum", out _), "exclusiveMinimum should not be present on broken schema");
            return;
        }
        Assert.Equal("integer", StringOrNull(schema, "type"));
        Assert.Equal("int32", StringOrNull(schema, "format"));
        AssertExclusiveLowerBound(schema, 0m, version);
    }

    private static void AssertEmailSchema(JsonElement schema, bool isBroken, bool isEmailFormatBroken, bool assertInline)
    {
        if (assertInline) AssertInlineSchema(schema);
        Assert.Equal("string", StringOrNull(schema, "type"));
        if (isBroken)
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
        if (!isEmailFormatBroken)
            Assert.Equal("email", StringOrNull(schema, "format"));
    }

    /// <summary>
    /// Looks up a named property on a form request-body schema. Pipelines
    /// disagree on casing (Microsoft PascalCase, Swashbuckle camelCase);
    /// the lookup is case-insensitive so tests aren't pipeline-flavoured.
    /// </summary>
    private static JsonElement GetFormProperty(JsonElement formSchema, string propertyName)
    {
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

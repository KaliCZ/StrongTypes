using System.Text.Json;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ExclusiveBounds;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Schema-shape assertions for parameters bound from non-body sources
/// (<c>[FromQuery]</c>, <c>[FromRoute]</c>, <c>[FromHeader]</c>) and for
/// individual properties of <c>[FromForm]</c> request bodies. Each assert
/// takes the relevant per-pipeline "broken" flags so the same assertion
/// runs on every pipeline — when a flag is set, the helper falls back to
/// asserting the underlying-only shape so the suite documents what the
/// pipeline actually emits today.
/// </summary>
internal static class BindingSchemaAsserts
{
    internal static void AssertNonBodyNonEmptyString(JsonElement schema, bool isBroken)
    {
        if (isBroken)
        {
            // Pipeline emits an underlying-type schema with no strong-type
            // constraints. Different sources (query, header, route) produce
            // different "wrong" shapes — just confirm the slot is wired up.
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        AssertInlineSchema(schema);
        Assert.Equal("string", StringOrNull(schema, "type"));
        Assert.Equal(1, IntOrNull(schema, "minLength"));
    }

    internal static void AssertNonBodyPositiveInt(JsonElement schema, bool isBroken, OpenApiVersion version)
    {
        if (isBroken)
        {
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        AssertInlineSchema(schema);
        Assert.Equal("integer", StringOrNull(schema, "type"));
        Assert.Equal("int32", StringOrNull(schema, "format"));
        AssertExclusiveLowerBound(schema, 0m, version);
    }

    internal static void AssertNonBodyEmail(JsonElement schema, bool isBroken, bool isEmailFormatBroken)
    {
        if (isBroken)
        {
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        AssertInlineSchema(schema);
        Assert.Equal("string", StringOrNull(schema, "type"));
        Assert.Equal(1, IntOrNull(schema, "minLength"));
        Assert.Equal(254, IntOrNull(schema, "maxLength"));
        if (!isEmailFormatBroken)
            Assert.Equal("email", StringOrNull(schema, "format"));
    }

    /// <summary>
    /// Looks up a named property on a form request-body schema. Returns
    /// <c>null</c> when <paramref name="isFormPropertiesBroken"/> is set —
    /// the pipeline mangled the form schema's <c>properties</c> map (e.g.
    /// emitted <c>allOf</c>-of-property-schemas with names dropped) and we
    /// can't navigate to a specific property by name. Pipelines disagree on
    /// casing (Microsoft PascalCase, Swashbuckle camelCase); the lookup is
    /// case-insensitive so tests aren't pipeline-flavoured.
    /// </summary>
    private static JsonElement? TryGetFormProperty(JsonElement formSchema, string propertyName, bool isFormPropertiesBroken)
    {
        if (isFormPropertiesBroken)
        {
            Assert.Equal(JsonValueKind.Object, formSchema.ValueKind);
            return null;
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

    internal static void AssertFormPropertyNonEmptyString(JsonElement formSchema, string propertyName, bool isFormPropertiesBroken, bool isStrongTypeBroken)
    {
        if (TryGetFormProperty(formSchema, propertyName, isFormPropertiesBroken) is not { } schema) return;
        if (isStrongTypeBroken)
        {
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        Assert.Equal("string", StringOrNull(schema, "type"));
        Assert.Equal(1, IntOrNull(schema, "minLength"));
    }

    internal static void AssertFormPropertyPositiveInt(JsonElement formSchema, string propertyName, bool isFormPropertiesBroken, bool isStrongTypeBroken, OpenApiVersion version)
    {
        if (TryGetFormProperty(formSchema, propertyName, isFormPropertiesBroken) is not { } schema) return;
        if (isStrongTypeBroken)
        {
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        Assert.Equal("integer", StringOrNull(schema, "type"));
        Assert.Equal("int32", StringOrNull(schema, "format"));
        AssertExclusiveLowerBound(schema, 0m, version);
    }

    internal static void AssertFormPropertyEmail(JsonElement formSchema, string propertyName, bool isFormPropertiesBroken, bool isStrongTypeBroken, bool isEmailFormatBroken)
    {
        if (TryGetFormProperty(formSchema, propertyName, isFormPropertiesBroken) is not { } schema) return;
        if (isStrongTypeBroken)
        {
            Assert.Equal(JsonValueKind.Object, schema.ValueKind);
            return;
        }
        Assert.Equal("string", StringOrNull(schema, "type"));
        Assert.Equal(1, IntOrNull(schema, "minLength"));
        Assert.Equal(254, IntOrNull(schema, "maxLength"));
        if (!isEmailFormatBroken)
            Assert.Equal("email", StringOrNull(schema, "format"));
    }
}

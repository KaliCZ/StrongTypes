using System.Text.Json;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// Version-strict assertions for exclusive numeric bounds. The two
/// OpenAPI versions encode an exclusive bound differently:
/// <c>3.0 → minimum: &lt;n&gt;</c> paired with <c>exclusiveMinimum: true</c>
/// (boolean), <c>3.1 → exclusiveMinimum: &lt;n&gt;</c> (numeric, no
/// companion <c>minimum</c>). These helpers pin the encoding for the
/// version under test; an emission in the other version's form fails
/// the assertion.
/// </summary>
internal static class ExclusiveBounds
{
    /// <summary>
    /// Asserts the schema carries an exclusive lower bound equal to
    /// <paramref name="expected"/>, encoded in the form required by
    /// <paramref name="version"/>.
    /// </summary>
    internal static void AssertExclusiveLowerBound(JsonElement schema, decimal expected, OpenApiVersion version)
    {
        Assert.True(
            schema.TryGetProperty("exclusiveMinimum", out var ex),
            "exclusiveMinimum is missing");

        if (version == OpenApiVersion.V3_1)
        {
            Assert.Equal(JsonValueKind.Number, ex.ValueKind);
            Assert.Equal(expected, ex.GetDecimal());
        }
        else
        {
            Assert.Equal(JsonValueKind.True, ex.ValueKind);
            Assert.True(
                schema.TryGetProperty("minimum", out var min) && min.ValueKind == JsonValueKind.Number,
                "minimum companion is missing for 3.0 exclusive lower bound");
            Assert.Equal(expected, min.GetDecimal());
        }
    }

    /// <summary>
    /// Asserts the schema carries an exclusive upper bound equal to
    /// <paramref name="expected"/>, encoded in the form required by
    /// <paramref name="version"/>.
    /// </summary>
    internal static void AssertExclusiveUpperBound(JsonElement schema, decimal expected, OpenApiVersion version)
    {
        Assert.True(
            schema.TryGetProperty("exclusiveMaximum", out var ex),
            "exclusiveMaximum is missing");

        if (version == OpenApiVersion.V3_1)
        {
            Assert.Equal(JsonValueKind.Number, ex.ValueKind);
            Assert.Equal(expected, ex.GetDecimal());
        }
        else
        {
            Assert.Equal(JsonValueKind.True, ex.ValueKind);
            Assert.True(
                schema.TryGetProperty("maximum", out var max) && max.ValueKind == JsonValueKind.Number,
                "maximum companion is missing for 3.0 exclusive upper bound");
            Assert.Equal(expected, max.GetDecimal());
        }
    }

    /// <summary>
    /// Asserts an exclusive lower bound equal to <paramref name="expected"/>
    /// is reachable from any layer of <paramref name="schema"/> via
    /// $ref/allOf/oneOf/anyOf, encoded in the form required by
    /// <paramref name="version"/>. Fails if no layer carries the bound.
    /// </summary>
    internal static void AssertExclusiveLowerBoundReachable(JsonElement doc, JsonElement schema, decimal expected, OpenApiVersion version)
    {
        foreach (var layer in SchemaWalk.WalkSchemaLayers(doc, schema, version))
        {
            if (!layer.TryGetProperty("exclusiveMinimum", out var ex)) continue;

            if (version == OpenApiVersion.V3_1)
            {
                if (ex.ValueKind == JsonValueKind.Number && ex.GetDecimal() == expected) return;
            }
            else
            {
                if (ex.ValueKind == JsonValueKind.True
                    && layer.TryGetProperty("minimum", out var min)
                    && min.ValueKind == JsonValueKind.Number
                    && min.GetDecimal() == expected) return;
            }
        }

        Assert.Fail($"Expected {version} exclusive lower bound of {expected} reachable from this schema, but none found.");
    }
}

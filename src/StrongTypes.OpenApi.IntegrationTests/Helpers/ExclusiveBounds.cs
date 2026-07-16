using System.Text.Json;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Helpers;

/// <summary>
/// The two OpenAPI versions encode an exclusive bound differently: 3.0 as <c>minimum: n</c> paired with a boolean
/// <c>exclusiveMinimum: true</c>, 3.1 as a numeric <c>exclusiveMinimum: n</c> with no companion <c>minimum</c>.
/// </summary>
internal static class ExclusiveBounds
{
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

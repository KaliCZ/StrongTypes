using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ExclusiveBounds;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaWalk;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task Positive_Int_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/positive-int-entities"));
        var value = Property(body, "value");

        AssertInlineSchema(value);
        Assert.Equal("integer", StringOrNull(value, "type"));
        Assert.Equal("int32", StringOrNull(value, "format"));
        AssertExclusiveLowerBound(value, 0m, Version);
    }

    [Fact]
    public async Task NonNegative_Long_Renders_As_Integer_Int64_With_Minimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/non-negative-long-entities"));
        var value = Property(body, "value");

        AssertInlineSchema(value);
        Assert.Equal("integer", StringOrNull(value, "type"));
        Assert.Equal("int64", StringOrNull(value, "format"));
        Assert.Equal(0m, DecimalOrNull(value, "minimum"));
        Assert.False(BoolOrFalse(value, "exclusiveMinimum"));
    }

    [Fact]
    public async Task Negative_Double_Renders_As_Number_Double_With_ExclusiveMaximum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/negative-double-entities"));
        var value = Property(body, "value");

        AssertInlineSchema(value);
        Assert.Equal("number", StringOrNull(value, "type"));
        Assert.Equal("double", StringOrNull(value, "format"));
        AssertExclusiveUpperBound(value, 0m, Version);
    }

    [Fact]
    public async Task NonPositive_Decimal_Renders_With_Maximum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/non-positive-decimal-entities"));
        var value = Property(body, "value");

        AssertInlineSchema(value);
        Assert.Equal("number", StringOrNull(value, "type"));
        Assert.Equal(0m, DecimalOrNull(value, "maximum"));
        Assert.False(BoolOrFalse(value, "exclusiveMaximum"));
    }

    [Fact]
    public async Task Nullable_Positive_Int_Property_Still_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/positive-int-entities"));
        var nullableValue = UnwrapNullableProperty(Property(body, "nullableValue"), Version);

        AssertInlineSchema(nullableValue);
        Assert.Equal("integer", StringOrNull(nullableValue, "type"));
        Assert.Equal("int32", StringOrNull(nullableValue, "format"));
        AssertExclusiveLowerBound(nullableValue, 0m, Version);
    }

    [Fact]
    public async Task Nullable_Positive_Int_On_Dedicated_Nullables_Endpoint_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nullable-strong-types"));
        var value = UnwrapNullableProperty(Property(body, "nullablePositiveInt"), Version);

        AssertInlineSchema(value);
        Assert.Equal("integer", StringOrNull(value, "type"));
        Assert.Equal("int32", StringOrNull(value, "format"));
        AssertExclusiveLowerBound(value, 0m, Version);
    }
}

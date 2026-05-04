using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.BindingSchemaAsserts;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task Positive_Int_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/positive-int-entities"));
        AssertPositiveIntSchema(Property(body, "value"), Version);
    }

    [Fact]
    public async Task NonNegative_Long_Renders_As_Integer_Int64_With_Minimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/non-negative-long-entities"));
        AssertNonNegativeLongSchema(Property(body, "value"));
    }

    [Fact]
    public async Task Negative_Double_Renders_As_Number_Double_With_ExclusiveMaximum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/negative-double-entities"));
        AssertNegativeDoubleSchema(Property(body, "value"), Version);
    }

    [Fact]
    public async Task NonPositive_Decimal_Renders_With_Maximum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/non-positive-decimal-entities"));
        AssertPlainDecimalSchema(Property(body, "plainValue"), Version);
        AssertNonPositiveDecimalSchema(Property(body, "value"));
    }

    [Fact]
    public async Task Nullable_Positive_Int_Property_Still_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/positive-int-entities"));
        AssertPositiveIntSchema(UnwrapNullableProperty(Property(body, "nullableValue"), Version), Version);
    }

    [Fact]
    public async Task Nullable_Positive_Int_On_Dedicated_Nullables_Endpoint_Renders_As_Integer_With_ExclusiveMinimum_Zero()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nullable-strong-types"));
        AssertPositiveIntSchema(UnwrapNullableProperty(Property(body, "nullablePositiveInt"), Version), Version);
    }
}

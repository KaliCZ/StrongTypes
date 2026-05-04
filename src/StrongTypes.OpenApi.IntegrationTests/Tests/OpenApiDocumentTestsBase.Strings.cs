using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.BindingSchemaAsserts;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task NonEmptyString_Renders_As_String_With_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/non-empty-string-entities"));
        AssertNonEmptyStringSchema(Property(body, "value"));
    }

    [Fact]
    public async Task Nullable_NonEmptyString_Property_Still_Renders_As_String_With_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/non-empty-string-entities"));
        AssertNonEmptyStringSchema(UnwrapNullableProperty(Property(body, "nullableValue"), Version));
    }

    [Fact]
    public async Task Nullable_NonEmptyString_On_Dedicated_Nullables_Endpoint_Renders_As_String_With_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nullable-strong-types"));
        AssertNonEmptyStringSchema(UnwrapNullableProperty(Property(body, "nullableNonEmptyString"), Version));
    }
}

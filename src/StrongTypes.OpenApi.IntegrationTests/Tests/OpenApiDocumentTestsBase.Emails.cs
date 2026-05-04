using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.BindingSchemaAsserts;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task Email_Renders_As_String_With_Format_Email_And_Length_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/email-entities"));
        AssertEmailSchema(Property(body, "value"));
    }

    [Fact]
    public async Task Nullable_Email_Property_Still_Renders_As_String_With_Format_Email_And_Length_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/email-entities"));
        AssertEmailSchema(UnwrapNullableProperty(Property(body, "nullableValue"), Version));
    }
}

using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaWalk;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task Email_Renders_As_String_With_Format_Email_And_Length_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/email-entities"));
        var value = Property(body, "value");

        AssertInlineSchema(value);
        Assert.Equal("string", StringOrNull(value, "type"));
        Assert.Equal("email", StringOrNull(value, "format"));
        Assert.Equal(1, IntOrNull(value, "minLength"));
        Assert.Equal(254, IntOrNull(value, "maxLength"));
        Assert.False(value.TryGetProperty("properties", out _));
    }

    [Fact]
    public async Task Nullable_Email_Property_Still_Renders_As_String_With_Format_Email_And_Length_Bounds()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/email-entities"));
        var nullableValue = UnwrapNullableProperty(Property(body, "nullableValue"), Version);

        AssertInlineSchema(nullableValue);
        Assert.Equal("string", StringOrNull(nullableValue, "type"));
        Assert.Equal("email", StringOrNull(nullableValue, "format"));
        Assert.Equal(1, IntOrNull(nullableValue, "minLength"));
        Assert.Equal(254, IntOrNull(nullableValue, "maxLength"));
    }
}

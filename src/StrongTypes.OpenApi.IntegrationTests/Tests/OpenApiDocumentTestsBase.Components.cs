using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ComponentSchemas;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public abstract partial class OpenApiDocumentTestsBase
{
    // Required-array contract — strong-type wrapper properties land in
    // the `required` array iff their primitive equivalent does. Whatever
    // the underlying pipeline produces for `string`, `int`, `string?`,
    // `[Required] string`, `required string` etc. is what it must produce
    // for the matching `NonEmptyString` / `Positive<int>` property.
    [Theory]
    [InlineData("plain",            "plainRaw")]
    [InlineData("nullable",         "nullableRaw")]
    [InlineData("withAttribute",    "withAttributeRaw")]
    [InlineData("attributeNullable","attributeNullableRaw")]
    [InlineData("withKeyword",      "withKeywordRaw")]
    [InlineData("keywordNullable",  "keywordNullableRaw")]
    public async Task Required_Membership_Matches_Underlying_Primitive(string strongName, string rawName)
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/required-variants"));
        var required = ReadRequiredArray(body);

        Assert.Equal(required.Contains(rawName), required.Contains(strongName));
    }

    // Components cleanup — every inlineable wrapper (NonEmptyString and
    // the numeric/array generics) must be removed from
    // components.schemas after the inliner runs. Maybe<T> is the one
    // intentional exception: its object-shaped wire form is worth
    // keeping as a named component.
    [Fact]
    public async Task Inlineable_Wrapper_Components_Are_Removed_From_Components_Schemas()
    {
        var doc = await GetDocumentAsync();
        var schemaNames = ReadComponentSchemaNames(doc);

        Assert.DoesNotContain("NonEmptyString", schemaNames);

        // Microsoft prefix style and Swashbuckle suffix style: any name
        // that begins or ends with one of the wrapper roots must be gone.
        foreach (var name in schemaNames)
        {
            Assert.False(
                IsInlineableWrapperName(name),
                $"Inlineable wrapper component '{name}' should have been removed from components.schemas.");
        }
    }
}

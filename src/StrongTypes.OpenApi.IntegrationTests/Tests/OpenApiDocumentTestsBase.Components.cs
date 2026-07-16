using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ComponentSchemas;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public abstract partial class OpenApiDocumentTestsBase
{
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

    [Fact]
    public async Task Inlineable_Wrapper_Components_Are_Removed_From_Components_Schemas()
    {
        var doc = await GetDocumentAsync();
        var schemaNames = ReadComponentSchemaNames(doc);

        Assert.DoesNotContain("NonEmptyString", schemaNames);

        foreach (var name in schemaNames)
        {
            Assert.False(
                IsInlineableWrapperName(name),
                $"Inlineable wrapper component '{name}' should have been removed from components.schemas.");
        }
    }

    // Inlining a wrapper orphans its generated storage component (e.g. MailAddress behind Email); an orphan left
    // behind leaks into consumer codegen as an unused type.
    [Fact]
    public async Task No_Component_Schema_Is_Left_Unreferenced_After_Inlining()
    {
        var doc = await GetDocumentAsync();
        var referenced = ReadReferencedSchemaNames(doc);

        foreach (var name in ReadComponentSchemaNames(doc))
        {
            Assert.True(
                referenced.Contains(name),
                $"Component schema '{name}' is orphaned — no $ref points at it after inlining.");
        }
    }
}

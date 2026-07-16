using Microsoft.OpenApi;
using StrongTypes.OpenApi.Core;
using Xunit;

namespace StrongTypes.OpenApi.Core.Tests;

/// <summary>
/// The storage-component cleanup after inlining touches only the known generated storage types (e.g. MailAddress
/// behind Email) — never a consumer's own schema.
/// </summary>
public sealed class StrongTypeInlinerScopingTests
{
    [Fact]
    public void Removes_Unreferenced_Storage_Component_But_Preserves_Unrelated_Orphan()
    {
        var profileDto = ObjectWith(("email", new OpenApiSchemaReference("Email")));
        var document = DocumentWith(
            ("Email", InlineableEmail()),
            ("MailAddress", ObjectWith(("address", String()))),
            ("OrphanDto", ObjectWith(("name", String()))),
            ("ProfileDto", profileDto));

        StrongTypeInliner.Inline(document);
        var schemas = document.Components!.Schemas!;

        Assert.False(schemas.ContainsKey("Email"), "the inlined wrapper itself is removed");
        Assert.False(schemas.ContainsKey("MailAddress"), "the unreferenced storage type is GC'd");
        Assert.True(schemas.ContainsKey("OrphanDto"), "an unrelated, unreferenced consumer schema must be left untouched");

        var inlinedEmail = Assert.IsType<OpenApiSchema>(profileDto.Properties!["email"]);
        Assert.True(inlinedEmail.Type == JsonSchemaType.String);
    }

    [Fact]
    public void Keeps_Storage_Component_A_Consumer_Still_References()
    {
        var document = DocumentWith(
            ("Email", InlineableEmail()),
            ("MailAddress", ObjectWith(("address", String()))),
            ("ContactDto", ObjectWith(("addr", new OpenApiSchemaReference("MailAddress")))));

        StrongTypeInliner.Inline(document);

        Assert.True(document.Components!.Schemas!.ContainsKey("MailAddress"),
            "a storage type still referenced by a consumer schema must be kept");
    }

    private static OpenApiSchema InlineableEmail()
    {
        var email = new OpenApiSchema { Type = JsonSchemaType.String, Format = "email", MinLength = 1, MaxLength = 254 };
        StrongTypeInlineMarker.Set(email);
        return email;
    }

    private static OpenApiSchema String() => new() { Type = JsonSchemaType.String };

    private static OpenApiSchema ObjectWith(params (string Name, IOpenApiSchema Schema)[] properties) => new()
    {
        Type = JsonSchemaType.Object,
        Properties = properties.ToDictionary(p => p.Name, p => p.Schema, StringComparer.Ordinal),
    };

    private static OpenApiDocument DocumentWith(params (string Name, IOpenApiSchema Schema)[] schemas) => new()
    {
        Components = new OpenApiComponents
        {
            Schemas = schemas.ToDictionary(s => s.Name, s => s.Schema, StringComparer.Ordinal),
        },
    };
}

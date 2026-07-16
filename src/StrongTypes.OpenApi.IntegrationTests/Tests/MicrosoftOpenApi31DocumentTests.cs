using StrongTypes.OpenApi.IntegrationTests.Helpers;
using StrongTypes.OpenApi.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public sealed class MicrosoftOpenApi31DocumentTests(MicrosoftTestApi31Factory factory)
    : OpenApiDocumentTestsBase(factory.CreateClient()), IClassFixture<MicrosoftTestApi31Factory>
{
    protected override string DocumentUrl => "/openapi/v1.json";
    protected override OpenApiVersion Version => OpenApiVersion.V3_1;
    protected override bool IsEmailAddressFormatIgnored => true;
}

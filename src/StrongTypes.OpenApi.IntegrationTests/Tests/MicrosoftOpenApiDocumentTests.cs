using StrongTypes.OpenApi.IntegrationTests.Helpers;
using StrongTypes.OpenApi.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public sealed class MicrosoftOpenApiDocumentTests(MicrosoftTestApiFactory factory)
    : OpenApiDocumentTestsBase(factory.CreateClient()), IClassFixture<MicrosoftTestApiFactory>
{
    protected override string DocumentUrl => "/openapi/v1.json";
    protected override OpenApiVersion Version => OpenApiVersion.V3_0;
    protected override bool IsEmailAddressFormatIgnored => true;
}

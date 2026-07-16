using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.OpenApi.TestApi.Swashbuckle;

namespace StrongTypes.OpenApi.IntegrationTests.Infrastructure;

public sealed class SwashbuckleTestApiFactory : WebApplicationFactory<SwashbuckleTestApiEntryPoint>;

using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.OpenApi.TestApi.Microsoft;

namespace StrongTypes.OpenApi.IntegrationTests.Infrastructure;

public sealed class MicrosoftTestApiFactory : WebApplicationFactory<MicrosoftTestApiEntryPoint>;

using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.AspNetCore.TestApi;

namespace StrongTypes.AspNetCore.IntegrationTests.Infrastructure;

public sealed class AspNetCoreTestApiFactory : WebApplicationFactory<AspNetCoreTestApiEntryPoint>;

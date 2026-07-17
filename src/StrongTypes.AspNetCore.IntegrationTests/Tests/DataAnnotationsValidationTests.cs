using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using StrongTypes.AspNetCore.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.AspNetCore.IntegrationTests.Tests;

/// <summary>
/// DataAnnotations on strong-typed properties through the real MVC pipeline.
/// The layering under test: the type's own invariant is enforced by the JSON
/// converter at binding (an invalid value never reaches validation), while the
/// attributes add presence (<c>[Required]</c>) and narrowing (<c>[Range]</c>) on
/// top — and <c>[Range]</c>'s numeric form must evaluate the wrapped value, not
/// reject every wrapper as unconvertible.
/// </summary>
public sealed class DataAnnotationsValidationTests(AspNetCoreTestApiFactory factory)
    : IClassFixture<AspNetCoreTestApiFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _client.Dispose();

    private async Task<HttpResponseMessage> Post(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client.PostAsync("/validation-probe/range", content, Ct);
    }

    private static async Task<string[]> ErrorsFor(HttpResponseMessage response, string key)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        return problem.GetProperty("errors").GetProperty(key).EnumerateArray()
            .Select(e => e.GetString()!).ToArray();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task InRangeWrapper_PassesValidation(int quantity)
    {
        var response = await Post($$"""{"quantity":{{quantity}}}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(101)]
    [InlineData(150)]
    public async Task OutOfRangeWrapper_FailsWithTheRangeMessage(int quantity)
    {
        var response = await Post($$"""{"quantity":{{quantity}}}""");

        var errors = await ErrorsFor(response, "Quantity");
        Assert.Contains("The field Quantity must be between 1 and 100.", errors);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"quantity":null}""")]
    public async Task MissingWrapper_FailsRequired(string json)
    {
        var response = await Post(json);

        var errors = await ErrorsFor(response, "Quantity");
        Assert.Contains("The Quantity field is required.", errors);
    }

    // 0 fails the type's own invariant at binding — [Range] never runs.
    [Fact]
    public async Task InvalidForTheType_FailsAtBinding()
    {
        var response = await Post("""{"quantity":0}""");

        var errors = await ErrorsFor(response, "Quantity");
        Assert.DoesNotContain("The field Quantity must be between 1 and 100.", errors);
    }
}

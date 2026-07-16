using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using StrongTypes.Api.Models;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class FiniteIntervalEntityApiTests(TestWebApplicationFactory factory)
    : IntervalEntityTests<FiniteIntervalEntity, FiniteInterval<int>>(factory)
{
    protected override string RoutePrefix => "finite-interval-entities";

    protected override object ValidBody => new { Start = 1, End = 10 };
    protected override FiniteInterval<int> ValidValue => FiniteInterval.Create(1, 10);

    protected override object UpdatedBody => new { Start = 20, End = 30 };
    protected override FiniteInterval<int> UpdatedValue => FiniteInterval.Create(20, 30);

    protected override object StartAfterEndBody => new { Start = 10, End = 1 };

    protected override object? NullRequiredEndpointBody => new { Start = (int?)null, End = 5 };

    protected override object? OmittedRequiredEndpointBody => new { Start = 1 };

    [Fact]
    public async Task ExclusiveBoundFlag_RoundTripsThroughTheWireAndTheJsonColumn()
    {
        var response = await Client.PostAsJsonAsync(
            "/finite-interval-entities",
            new { value = new { Start = 1, End = 10, EndInclusive = false }, nullableValue = (object?)null },
            Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EntityResponse>(Ct);

        await AssertEntity(created!.Id, FiniteInterval.Create(1, 10, endInclusive: false), null);

        var json = await Client.GetFromJsonAsync<JsonElement>($"/finite-interval-entities/{created.Id}/postgresql", Ct);
        var value = json.GetProperty("value");
        Assert.False(value.GetProperty("endInclusive").GetBoolean());
        Assert.False(value.TryGetProperty("startInclusive", out _));   // default bounds stay off the wire
    }

    [Fact]
    public async Task EqualEndpointsWithAnExclusiveBound_AreRejected()
    {
        var response = await Client.PostAsJsonAsync(
            "/finite-interval-entities",
            new { value = new { Start = 5, End = 5, EndInclusive = false }, nullableValue = (object?)null },
            Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

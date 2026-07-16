using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using StrongTypes.Api.Models;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalEntityApiTests(TestWebApplicationFactory factory)
    : IntervalEntityTests<IntervalEntity, Interval<int>>(factory)
{
    protected override string RoutePrefix => "interval-entities";

    protected override object ValidBody => new { Start = 1, End = 10 };
    protected override Interval<int> ValidValue => Interval.Create(1, 10);

    // Updating to the fully-unbounded interval exercises both endpoints being null.
    protected override object UpdatedBody => new { Start = (int?)null, End = (int?)null };
    protected override Interval<int> UpdatedValue => Interval.Create<int>(null, null);

    protected override object StartAfterEndBody => new { Start = 10, End = 1 };

    [Fact]
    public async Task OpenEndedInterval_SerializesAbsentEndpointsAsJsonNull()
    {
        var response = await Client.PostAsJsonAsync(
            "/interval-entities", new { value = new { Start = 5, End = (int?)null }, nullableValue = (object?)null }, Ct);
        var created = await response.Content.ReadFromJsonAsync<EntityResponse>(Ct);

        var json = await Client.GetFromJsonAsync<JsonElement>($"/interval-entities/{created!.Id}/postgresql", Ct);
        var value = json.GetProperty("value");
        Assert.Equal(5, value.GetProperty("start").GetInt32());
        Assert.Equal(JsonValueKind.Null, value.GetProperty("end").ValueKind);
    }

    [Fact]
    public async Task EmptyObject_IsAcceptedAsUnboundedInterval()
    {
        var response = await Client.PostAsJsonAsync(
            "/interval-entities", new { value = new { }, nullableValue = (object?)null }, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EntityResponse>(Ct);

        var json = await Client.GetFromJsonAsync<JsonElement>($"/interval-entities/{created!.Id}/postgresql", Ct);
        var value = json.GetProperty("value");
        Assert.Equal(JsonValueKind.Null, value.GetProperty("start").ValueKind);
        Assert.Equal(JsonValueKind.Null, value.GetProperty("end").ValueKind);
    }
}

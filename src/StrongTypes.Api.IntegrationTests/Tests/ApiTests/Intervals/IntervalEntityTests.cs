using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using StrongTypes.Api.Models;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Shared wire-to-DB suite for the four interval entities. Unlike the scalar
/// strong types, an interval is an object on the wire (<c>{ "start": …, "end": … }</c>),
/// so this base is purpose-built rather than reusing the scalar
/// <c>EntityTests</c> harness. Each variant plugs in valid/updated/invalid
/// bodies and asserts the full path: System.Text.Json converter → ASP.NET
/// pipeline → <c>IntervalJsonValueConverter</c> against both providers.
/// </summary>
public abstract class IntervalEntityTests<TEntity, TInterval>(TestWebApplicationFactory factory)
    : IntegrationTestBase<TEntity, TInterval, TInterval?>(factory)
    where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
    where TInterval : struct
{
    private static readonly JsonSerializerOptions WireJson = new(JsonSerializerDefaults.Web);

    protected abstract string RoutePrefix { get; }

    /// <summary>A valid interval as an anonymous wire body, plus its strong-type form.</summary>
    protected abstract object ValidBody { get; }
    protected abstract TInterval ValidValue { get; }

    /// <summary>A second valid interval distinct from <see cref="ValidValue"/>, for update tests.</summary>
    protected abstract object UpdatedBody { get; }
    protected abstract TInterval UpdatedValue { get; }

    /// <summary>A wire body whose endpoints violate <c>Start &lt;= End</c>. Every variant has one.</summary>
    protected abstract object StartAfterEndBody { get; }

    /// <summary>
    /// A wire body that sends <c>null</c> for an endpoint the variant requires,
    /// or <see langword="null"/> when the variant has no required endpoint
    /// (<see cref="Interval{T}"/>). Used to assert the framework rejects a
    /// missing-but-required endpoint with a 400.
    /// </summary>
    protected virtual object? NullRequiredEndpointBody => null;

    /// <summary>
    /// A wire body that omits a required endpoint key entirely (vs. sending it as
    /// <c>null</c>), or <see langword="null"/> when the variant has no required
    /// endpoint (<see cref="Interval{T}"/>).
    /// </summary>
    protected virtual object? OmittedRequiredEndpointBody => null;

    private string CreateEndpoint => $"/{RoutePrefix}";
    private string UpdateEndpoint(Guid id) => $"/{RoutePrefix}/{id}";
    private string PatchEndpoint(Guid id) => $"/{RoutePrefix}/{id}";
    private string SqlServerGetEndpoint(Guid id) => $"/{RoutePrefix}/{id}/sql-server";
    private string PostgreSqlGetEndpoint(Guid id) => $"/{RoutePrefix}/{id}/postgresql";

    private async Task<Guid> PostValid(object value, object? nullableValue)
    {
        var response = await Client.PostAsJsonAsync(CreateEndpoint, new { value, nullableValue }, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EntityResponse>(Ct);
        return created!.Id;
    }

    private async Task<JsonElement> Get(string url)
    {
        var response = await Client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    private async Task<HttpResponseMessage> Patch(Guid id, object body)
    {
        using var content = JsonContent.Create(body);
        return await Client.PatchAsync(PatchEndpoint(id), content, Ct);
    }

    private static TInterval ReadInterval(JsonElement element) => element.Deserialize<TInterval>(WireJson);

    // ── Create + persist ─────────────────────────────────────────────────

    [Fact]
    public async Task ValidInterval_PersistsInBothDatabases()
    {
        var id = await PostValid(ValidBody, ValidBody);
        await AssertEntity(id, ValidValue, ValidValue);
    }

    [Fact]
    public async Task ValidValueWithNullNullable_PersistsInBothDatabases()
    {
        var id = await PostValid(ValidBody, null);
        await AssertEntity(id, ValidValue, null);
    }

    // ── Read ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_RoundTripsTheIntervalObjectFromBothDatabases()
    {
        var entity = TEntity.Create(ValidValue, ValidValue);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        if (SqlServerAvailable)
        {
            var sqlJson = await Get(SqlServerGetEndpoint(entity.Id));
            Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
            Assert.Equal(ValidValue, ReadInterval(sqlJson.GetProperty("value")));
            Assert.Equal(ValidValue, ReadInterval(sqlJson.GetProperty("nullableValue")));
        }

        var pgJson = await Get(PostgreSqlGetEndpoint(entity.Id));
        Assert.Equal(entity.Id, pgJson.GetProperty("id").GetGuid());
        Assert.Equal(ValidValue, ReadInterval(pgJson.GetProperty("value")));
        Assert.Equal(ValidValue, ReadInterval(pgJson.GetProperty("nullableValue")));
    }

    [Fact]
    public async Task Get_SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var entity = TEntity.Create(ValidValue, null);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        if (SqlServerAvailable)
        {
            var sqlJson = await Get(SqlServerGetEndpoint(entity.Id));
            Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
            Assert.Equal(ValidValue, ReadInterval(sqlJson.GetProperty("value")));
        }

        var pgJson = await Get(PostgreSqlGetEndpoint(entity.Id));
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal(ValidValue, ReadInterval(pgJson.GetProperty("value")));
    }

    // ── Update ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_PersistsNewIntervalInBothDatabases()
    {
        var id = await PostValid(ValidBody, ValidBody);
        var response = await Client.PutAsJsonAsync(
            UpdateEndpoint(id), new { value = UpdatedBody, nullableValue = (object?)null }, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertEntity(id, UpdatedValue, null);
    }

    // ── Patch ────────────────────────────────────────────────────────────
    // The interval controllers inherit PATCH from StructTypeEntityControllerBase;
    // these drive it with an interval-shaped body — the plumbing itself is already
    // covered by the scalar struct types.

    [Fact]
    public async Task Patch_ValueOnly_UpdatesValueLeavesNullableValueUnchanged()
    {
        var id = await PostValid(ValidBody, ValidBody);

        var response = await Patch(id, new { value = UpdatedBody });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(id, UpdatedValue, ValidValue);
    }

    [Fact]
    public async Task Patch_NullableValueSome_UpdatesNullableValueLeavesValueUnchanged()
    {
        var id = await PostValid(ValidBody, null);

        var response = await Patch(id, new { nullableValue = new { Value = UpdatedBody } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(id, ValidValue, UpdatedValue);
    }

    [Fact]
    public async Task Patch_NullableValueEmptyObject_ClearsNullableValue()
    {
        var id = await PostValid(ValidBody, ValidBody);

        var response = await Patch(id, new { nullableValue = new { } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(id, ValidValue, null);
    }

    [Fact]
    public async Task Patch_NonExistentId_ReturnsNotFound()
    {
        var response = await Patch(Guid.NewGuid(), new { value = UpdatedBody });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Invalid payloads ─────────────────────────────────────────────────

    [Fact]
    public async Task StartAfterEnd_InValue_ReturnsBadRequestKeyedByValuePath()
    {
        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = StartAfterEndBody, nullableValue = ValidBody }, Ct);
        var errors = await AssertValidationProblem(response);
        Assert.True(errors.TryGetProperty("$.value", out var messages));
        Assert.Contains("less than or equal", string.Join(" ", messages.EnumerateArray().Select(m => m.GetString())));
    }

    [Fact]
    public async Task StartAfterEnd_InNullableValue_ReturnsBadRequestKeyedByNullableValuePath()
    {
        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = ValidBody, nullableValue = StartAfterEndBody }, Ct);
        var errors = await AssertValidationProblem(response);
        Assert.True(errors.TryGetProperty("$.nullableValue", out _));
    }

    [Fact]
    public async Task OmittedRequiredEndpoint_InValue_ReturnsBadRequestKeyedByValuePath()
    {
        Assert.SkipWhen(OmittedRequiredEndpointBody is null, "Variant has no required endpoint.");

        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = OmittedRequiredEndpointBody, nullableValue = ValidBody }, Ct);
        var errors = await AssertValidationProblem(response);
        Assert.True(errors.TryGetProperty("$.value", out var messages));
        Assert.Contains("requires the", string.Join(" ", messages.EnumerateArray().Select(m => m.GetString())));
    }

    [Fact]
    public async Task NullValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = (object?)null, nullableValue = ValidBody }, Ct);
        var errors = await AssertValidationProblem(response);
        var keys = errors.EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Contains(keys, k => k is "$.value" or "value" or "Value");
    }

    [Fact]
    public async Task NullRequiredEndpoint_ReturnsBadRequest()
    {
        Assert.SkipWhen(NullRequiredEndpointBody is null, "Variant has no required endpoint.");

        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = NullRequiredEndpointBody, nullableValue = ValidBody }, Ct);
        var errors = await AssertValidationProblem(response);
        Assert.True(errors.TryGetProperty("$.value", out _));
    }

    private static async Task<JsonElement> AssertValidationProblem(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.Equal("One or more validation errors occurred.", problem.GetProperty("title").GetString());
        var errors = problem.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        Assert.NotEmpty(errors.EnumerateObject());
        return errors;
    }
}

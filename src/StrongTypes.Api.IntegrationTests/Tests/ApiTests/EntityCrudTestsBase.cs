using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using StrongTypes.Api.Models;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// The create / get / update / PATCH surface — including the null / value / clear-nullable
/// semantics of the nullable slot — that every wire-to-DB entity test must cover, written
/// once so the two wire shapes cannot drift apart. A concrete harness supplies only the
/// wire-shape-specific pieces: the request body for a value
/// (<see cref="ValidBody"/> / <see cref="UpdatedBody"/>), its strong-typed form
/// (<see cref="ValidValue"/> / <see cref="UpdatedValue"/>), and the GET read assertion
/// (<see cref="AssertJsonIsValidValue"/>).
/// </summary>
/// <remarks>
/// Two harnesses derive from this base:
/// <see cref="EntityTests{TSelf, TEntity, T, TNullable, TWire}"/> for scalar wire types (a
/// value serializes as a single JSON scalar) and
/// <see cref="IntervalEntityTests{TEntity, TInterval}"/> for intervals (a value serializes
/// as a JSON object). Each adds only its own invalid-payload cases — a malformed scalar on
/// one side, <c>Start &gt; End</c> / a missing required endpoint on the other. The shared
/// scenarios below exist exactly once, so a scenario can no longer be present for one wire
/// shape and silently missing for the other.
/// </remarks>
public abstract class EntityCrudTestsBase<TEntity, T, TNullable>(TestWebApplicationFactory factory)
    : IntegrationTestBase<TEntity, T, TNullable>(factory)
    where TEntity : class, IEntity<TEntity, T, TNullable>
    where T : notnull
{
    /// <summary>Route segment this entity is exposed under, e.g. "positive-int-entities".</summary>
    protected abstract string RoutePrefix { get; }

    /// <summary>Request body for a valid value: a bare scalar, or a <c>{ start, end }</c> object.</summary>
    protected abstract object ValidBody { get; }

    /// <summary>The strong-typed form of <see cref="ValidBody"/>, for asserting persisted state.</summary>
    protected abstract T ValidValue { get; }

    /// <summary>A second valid value distinct from <see cref="ValidBody"/>, for update/PATCH tests.</summary>
    protected abstract object UpdatedBody { get; }

    /// <summary>The strong-typed form of <see cref="UpdatedBody"/>.</summary>
    protected abstract T UpdatedValue { get; }

    /// <summary>Asserts a GET response's <c>value</c>/<c>nullableValue</c> element equals <see cref="ValidValue"/>.</summary>
    protected abstract void AssertJsonIsValidValue(JsonElement element);

    protected string CreateEndpoint => $"/{RoutePrefix}";
    protected string UpdateEndpoint(Guid id) => $"/{RoutePrefix}/{id}";
    protected string PatchEndpoint(Guid id) => $"/{RoutePrefix}/{id}";
    protected string SqlServerGetEndpoint(Guid id) => $"/{RoutePrefix}/{id}/sql-server";
    protected string PostgreSqlGetEndpoint(Guid id) => $"/{RoutePrefix}/{id}/postgresql";

    protected async Task<EntityResponse> Post(string url, object body)
    {
        var response = await Client.PostAsJsonAsync(url, body, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EntityResponse>(Ct))!;
    }

    protected async Task Put(string url, object body)
    {
        var response = await Client.PutAsJsonAsync(url, body, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    protected async Task<HttpResponseMessage> Patch(string url, object body)
    {
        using var content = JsonContent.Create(body);
        return await Client.PatchAsync(url, content, Ct);
    }

    protected async Task<JsonElement> Get(string url)
    {
        var response = await Client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    // T → TNullable bridge. For struct T with TNullable = Nullable<T>, boxing then unboxing to
    // Nullable<T> is a supported CLR conversion; for class T with TNullable = T? it is identity.
    // default! is null in both shapes.
    protected static TNullable ToNullable(T value) => (TNullable)(object)value!;
    protected static TNullable NullNullable => default!;

    // ── Create ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidValue_PersistsInBothDatabases()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = ValidBody });
        await AssertEntity(created.Id, ValidValue, ToNullable(ValidValue));
    }

    [Fact]
    public async Task ValidValueWithNullNullable_PersistsInBothDatabases()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = (object?)null });
        await AssertEntity(created.Id, ValidValue, NullNullable);
    }

    // Value is non-nullable, so a null Value is a 400. The error key is mechanism-dependent: a
    // struct (or a reference converter that rejects null) fails at parse time -> "$.value"; a
    // reference converter that maps null through trips the post-binding implicit-required check
    // -> the C# name "Value". Accept the field key with or without the "$." prefix either way.
    [Fact]
    public async Task NullValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = (object?)null, nullableValue = ValidBody }, Ct);
        var errors = await AssertValidationProblem(response);
        var keys = errors.EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Contains(keys, k => k is "$.value" or "value" or "Value");
    }

    // ── Get ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsEntityWithCamelCaseJsonFromBothDatabases()
    {
        var entity = TEntity.Create(ValidValue, ToNullable(ValidValue));
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        if (SqlServerAvailable)
        {
            var sqlJson = await Get(SqlServerGetEndpoint(entity.Id));
            Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
            AssertJsonIsValidValue(sqlJson.GetProperty("value"));
            AssertJsonIsValidValue(sqlJson.GetProperty("nullableValue"));
        }

        var pgJson = await Get(PostgreSqlGetEndpoint(entity.Id));
        Assert.Equal(entity.Id, pgJson.GetProperty("id").GetGuid());
        AssertJsonIsValidValue(pgJson.GetProperty("value"));
        AssertJsonIsValidValue(pgJson.GetProperty("nullableValue"));
    }

    [Fact]
    public async Task Get_SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var entity = TEntity.Create(ValidValue, NullNullable);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        if (SqlServerAvailable)
        {
            var sqlJson = await Get(SqlServerGetEndpoint(entity.Id));
            Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
            AssertJsonIsValidValue(sqlJson.GetProperty("value"));
        }

        var pgJson = await Get(PostgreSqlGetEndpoint(entity.Id));
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        AssertJsonIsValidValue(pgJson.GetProperty("value"));
    }

    // ── Update ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_PersistsNewValueAndNullableValueInBothDatabases()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = ValidBody });
        await Put(UpdateEndpoint(created.Id), new { value = UpdatedBody, nullableValue = UpdatedBody });
        await AssertEntity(created.Id, UpdatedValue, ToNullable(UpdatedValue));
    }

    [Fact]
    public async Task Update_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = (object?)null });
        await Put(UpdateEndpoint(created.Id), new { value = ValidBody, nullableValue = UpdatedBody });
        await AssertEntity(created.Id, ValidValue, ToNullable(UpdatedValue));
    }

    [Fact]
    public async Task Update_ClearsNullableValueToNullInBothDatabases()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = ValidBody });
        await Put(UpdateEndpoint(created.Id), new { value = ValidBody, nullableValue = (object?)null });
        await AssertEntity(created.Id, ValidValue, NullNullable);
    }

    // ── Patch ────────────────────────────────────────────────────────────
    // Wire semantics per field:
    //   Value        — null/absent ⇒ skip;      non-null ⇒ update.
    //   NullableValue — null/absent ⇒ skip; { Value: x } ⇒ set x; {} or { Value: null } ⇒ clear.

    [Fact]
    public async Task Patch_EmptyBody_LeavesBothFieldsUnchanged()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = ValidBody });

        var response = await Patch(PatchEndpoint(created.Id), new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(created.Id, ValidValue, ToNullable(ValidValue));
    }

    [Fact]
    public async Task Patch_ValueOnly_UpdatesValueLeavesNullableValueUnchanged()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = ValidBody });

        var response = await Patch(PatchEndpoint(created.Id), new { value = UpdatedBody });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(created.Id, UpdatedValue, ToNullable(ValidValue));
    }

    [Fact]
    public async Task Patch_ExplicitNullValue_LeavesValueUnchanged()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = ValidBody });

        var response = await Patch(PatchEndpoint(created.Id),
            new { value = (object?)null, nullableValue = (object?)null });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(created.Id, ValidValue, ToNullable(ValidValue));
    }

    [Fact]
    public async Task Patch_NullableValueSome_UpdatesNullableValueLeavesValueUnchanged()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = (object?)null });

        var response = await Patch(PatchEndpoint(created.Id), new { nullableValue = new { Value = UpdatedBody } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(created.Id, ValidValue, ToNullable(UpdatedValue));
    }

    [Fact]
    public async Task Patch_NullableValueEmptyObject_ClearsNullableValue()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = ValidBody });

        var response = await Patch(PatchEndpoint(created.Id), new { nullableValue = new { } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(created.Id, ValidValue, NullNullable);
    }

    [Fact]
    public async Task Patch_NullableValueWithExplicitNullInner_ClearsNullableValue()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = ValidBody });

        var response = await Patch(PatchEndpoint(created.Id), new { nullableValue = new { Value = (object?)null } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(created.Id, ValidValue, NullNullable);
    }

    [Fact]
    public async Task Patch_UpdatesBothFieldsIndependently()
    {
        var created = await Post(CreateEndpoint, new { value = ValidBody, nullableValue = (object?)null });

        var response = await Patch(PatchEndpoint(created.Id),
            new { value = UpdatedBody, nullableValue = new { Value = UpdatedBody } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(created.Id, UpdatedValue, ToNullable(UpdatedValue));
    }

    [Fact]
    public async Task Patch_NonExistentId_ReturnsNotFound()
    {
        var response = await Patch(PatchEndpoint(Guid.NewGuid()), new { value = UpdatedBody });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Asserts a 400 carrying a well-formed <c>ValidationProblemDetails</c> (RFC 9457
    /// problem+json with a non-empty <c>errors</c> map) and returns the <c>errors</c>
    /// object so callers can assert specific keys.
    /// </summary>
    protected static async Task<JsonElement> AssertValidationProblem(HttpResponseMessage response)
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

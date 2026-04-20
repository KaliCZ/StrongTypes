using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using StrongTypes.Api.Models;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Compile-time contract every entity test class must satisfy. <c>static abstract</c>
/// members force each concrete subclass to supply the <see cref="TheoryData{T}"/>
/// lists — a missing one is a build error, not a silently skipped test. xUnit
/// resolves the actual members by reflection on the concrete type at discovery time.
/// </summary>
public interface IEntityTestData<TWire>
{
    static abstract TheoryData<TWire> ValidInputs { get; }
    static abstract TheoryData<TWire> InvalidInputs { get; }
}

/// <summary>
/// Shared Create/Get/Update suite for every <see cref="IEntity{TSelf, T, TNullable}"/>.
/// The test shape (valid inputs, invalid inputs, null handling) is identical for
/// numeric and string strong types; only the wire-format raw type
/// (<typeparamref name="TWire"/>) and the wrapper factory differ. Concrete
/// subclasses plug in a <c>Create(TWire)</c> factory, two seed values
/// (<see cref="FirstValid"/>, <see cref="UpdatedValid"/>), the <see cref="RoutePrefix"/>
/// inherited from <see cref="IntegrationTestBase{TEntity, T, TNullable}"/>, and
/// the two <see cref="TheoryData{T}"/> lists required by
/// <see cref="IEntityTestData{TWire}"/>.
/// </summary>
/// <remarks>
/// <typeparamref name="TSelf"/> uses the curiously recurring template pattern so
/// each concrete subclass passes itself and must implement
/// <see cref="IEntityTestData{TWire}"/> — a missing data member is a CS0535
/// build error instead of a silently skipped test.
/// </remarks>
public abstract class EntityTests<TSelf, TEntity, T, TNullable, TWire>(TestWebApplicationFactory factory)
    : IntegrationTestBase<TEntity, T, TNullable>(factory)
    where TSelf : EntityTests<TSelf, TEntity, T, TNullable, TWire>, IEntityTestData<TWire>
    where TEntity : class, IEntity<TEntity, T, TNullable>
    where T : notnull
{
    /// <summary>Route segment this entity is exposed under, e.g. "non-empty-string-entities".</summary>
    protected abstract string RoutePrefix { get; }

    protected string CreateEndpoint => $"/{RoutePrefix}";
    protected string UpdateEndpoint(Guid id) => $"/{RoutePrefix}/{id}";
    protected string PatchEndpoint(Guid id) => $"/{RoutePrefix}/{id}";
    protected string SqlServerGetEndpoint(Guid id) => $"/{RoutePrefix}/{id}/sql-server";
    protected string PostgreSqlGetEndpoint(Guid id) => $"/{RoutePrefix}/{id}/postgresql";

    /// <summary>
    /// Builds the { Value, NullableValue } request body used by every write endpoint.
    /// Generic over the wire types so tests can send plain scalars (int, string, …)
    /// regardless of the strong type the server binds them into.
    /// </summary>
    protected static object Body<TValue, TNullableValue>(TValue value, TNullableValue nullableValue) =>
        new { Value = value, NullableValue = nullableValue };

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

    /// <summary>Wraps a raw wire-format value in the strong type.</summary>
    protected abstract T Create(TWire raw);

    /// <summary>
    /// Seed valid value used as the "other" slot when testing one field in
    /// isolation, and as the baseline for Get/Update tests.
    /// </summary>
    protected abstract TWire FirstValid { get; }

    /// <summary>Target value for the update test; must differ from <see cref="FirstValid"/>.</summary>
    protected abstract TWire UpdatedValid { get; }

    // T → TNullable bridge. For struct T with TNullable = Nullable<T>, boxing
    // then unboxing to Nullable<T> is a supported CLR conversion. For class T
    // with TNullable = T? (NRT annotation), it's identity.
    private static TNullable ToNullable(T value) => (TNullable)(object)value!;

    // Default of TNullable is null in both shapes: Nullable<T>.default == null,
    // reference-type default == null.
    private static TNullable NullNullable => default!;

    // xUnit resolves these names on the concrete test class at runtime; the
    // analyzer can't see through the static abstract + CRTP, so we suppress
    // xUnit1015 where we reference them.
    protected const string ValidInputsMember = nameof(IEntityTestData<TWire>.ValidInputs);
    protected const string InvalidInputsMember = nameof(IEntityTestData<TWire>.InvalidInputs);

#pragma warning disable xUnit1015

    // ── Create: valid ────────────────────────────────────────────────────

    [Theory]
    [MemberData(ValidInputsMember)]
    public async Task ValidInput_PersistsInBothDatabases(TWire value)
    {
        var created = await Post(CreateEndpoint, new { value, nullableValue = value });
        var expected = Create(value);
        await AssertEntity(SqlSet, created.Id, expected, ToNullable(expected));
        await AssertEntity(PgSet, created.Id, expected, ToNullable(expected));
    }

    [Fact]
    public async Task ValidValueWithNullNullable_PersistsInBothDatabases()
    {
        var created = await Post(CreateEndpoint, new { value = (object?)FirstValid, nullableValue = (object?)null });
        await AssertEntity(SqlSet, created.Id, Create(FirstValid), NullNullable);
        await AssertEntity(PgSet, created.Id, Create(FirstValid), NullNullable);
    }

    // ── Create: invalid (non-null) ───────────────────────────────────────
    // Each invalid value is tested in both slots independently; the other
    // slot holds FirstValid so the test isolates the one field being checked.

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task InvalidValue_ReturnsBadRequest(TWire invalid)
    {
        var response = await Client.PostAsJsonAsync(CreateEndpoint, new { value = (object?)invalid, nullableValue = (object?)FirstValid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task InvalidNullableValue_ReturnsBadRequest(TWire invalid)
    {
        var response = await Client.PostAsJsonAsync(CreateEndpoint, new { value = (object?)FirstValid, nullableValue = (object?)invalid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
#pragma warning restore xUnit1015

    // ── Create: null ─────────────────────────────────────────────────────
    // Value is non-nullable, so null Value is a 400. Null NullableValue is the
    // legit case covered by ValidValueWithNullNullable_PersistsInBothDatabases.

    [Fact]
    public async Task NullValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = (object?)null, nullableValue = (object?)FirstValid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Get ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsEntityWithCamelCaseJsonFromBothDatabases()
    {
        var wrapped = Create(FirstValid);
        var entity = TEntity.Create(wrapped, ToNullable(wrapped));
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGetEndpoint(entity.Id));
        Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
        AssertJsonEquals(sqlJson.GetProperty("value"), FirstValid);
        AssertJsonEquals(sqlJson.GetProperty("nullableValue"), FirstValid);

        var pgJson = await Get(PostgreSqlGetEndpoint(entity.Id));
        Assert.Equal(entity.Id, pgJson.GetProperty("id").GetGuid());
        AssertJsonEquals(pgJson.GetProperty("value"), FirstValid);
        AssertJsonEquals(pgJson.GetProperty("nullableValue"), FirstValid);
    }

    [Fact]
    public async Task Get_SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var entity = TEntity.Create(Create(FirstValid), NullNullable);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGetEndpoint(entity.Id));
        Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
        AssertJsonEquals(sqlJson.GetProperty("value"), FirstValid);

        var pgJson = await Get(PostgreSqlGetEndpoint(entity.Id));
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        AssertJsonEquals(pgJson.GetProperty("value"), FirstValid);
    }

    // ── Update ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_PersistsNewValueAndNullableValueInBothDatabases()
    {
        var created = await Post(CreateEndpoint, new { value = FirstValid, nullableValue = FirstValid });
        await Put(UpdateEndpoint(created.Id), new { value = UpdatedValid, nullableValue = UpdatedValid });

        var updated = Create(UpdatedValid);
        await AssertEntity(SqlSet, created.Id, updated, ToNullable(updated));
        await AssertEntity(PgSet, created.Id, updated, ToNullable(updated));
    }

    [Fact]
    public async Task Update_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var created = await Post(CreateEndpoint, new { value = (object?)FirstValid, nullableValue = (object?)null });
        await Put(UpdateEndpoint(created.Id), new { value = (object?)FirstValid, nullableValue = (object?)UpdatedValid });

        await AssertEntity(SqlSet, created.Id, Create(FirstValid), ToNullable(Create(UpdatedValid)));
        await AssertEntity(PgSet, created.Id, Create(FirstValid), ToNullable(Create(UpdatedValid)));
    }

    [Fact]
    public async Task Update_ClearsNullableValueToNullInBothDatabases()
    {
        var created = await Post(CreateEndpoint, new { value = FirstValid, nullableValue = FirstValid });
        await Put(UpdateEndpoint(created.Id), new { value = (object?)FirstValid, nullableValue = (object?)null });

        await AssertEntity(SqlSet, created.Id, Create(FirstValid), NullNullable);
        await AssertEntity(PgSet, created.Id, Create(FirstValid), NullNullable);
    }

    // ── Patch ────────────────────────────────────────────────────────────
    //
    // Patch wire semantics follow JSON Merge Patch (RFC 7396): each field is
    // independently absent, null, or a value.
    //   Value        — absent/null ⇒ skip. A value ⇒ update.
    //                  (The field is non-nullable on the entity, so there is
    //                  nothing to clear; explicit null is a no-op.)
    //   NullableValue — absent ⇒ skip. null ⇒ clear. A value ⇒ update.

    [Fact]
    public async Task Patch_EmptyBody_LeavesBothFieldsUnchanged()
    {
        var created = await Post(CreateEndpoint, new { value = FirstValid, nullableValue = FirstValid });

        var response = await Patch(PatchEndpoint(created.Id), new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var expected = Create(FirstValid);
        await AssertEntity(SqlSet, created.Id, expected, ToNullable(expected));
        await AssertEntity(PgSet, created.Id, expected, ToNullable(expected));
    }

    [Fact]
    public async Task Patch_ValueOnly_UpdatesValueLeavesNullableValueUnchanged()
    {
        var created = await Post(CreateEndpoint, new { value = FirstValid, nullableValue = FirstValid });

        var response = await Patch(PatchEndpoint(created.Id), new { value = UpdatedValid });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(SqlSet, created.Id, Create(UpdatedValid), ToNullable(Create(FirstValid)));
        await AssertEntity(PgSet, created.Id, Create(UpdatedValid), ToNullable(Create(FirstValid)));
    }

    [Fact]
    public async Task Patch_ExplicitNullValue_LeavesValueUnchanged()
    {
        var created = await Post(CreateEndpoint, new { value = FirstValid, nullableValue = FirstValid });

        var response = await Patch(PatchEndpoint(created.Id), new { value = (object?)null });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var expected = Create(FirstValid);
        await AssertEntity(SqlSet, created.Id, expected, ToNullable(expected));
        await AssertEntity(PgSet, created.Id, expected, ToNullable(expected));
    }

    [Fact]
    public async Task Patch_NullableValueSome_UpdatesNullableValueLeavesValueUnchanged()
    {
        var created = await Post(CreateEndpoint, new { value = FirstValid, nullableValue = (object?)null });

        var response = await Patch(PatchEndpoint(created.Id), new { nullableValue = UpdatedValid });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(SqlSet, created.Id, Create(FirstValid), ToNullable(Create(UpdatedValid)));
        await AssertEntity(PgSet, created.Id, Create(FirstValid), ToNullable(Create(UpdatedValid)));
    }

    [Fact]
    public async Task Patch_NullableValueExplicitNull_ClearsNullableValue()
    {
        var created = await Post(CreateEndpoint, new { value = FirstValid, nullableValue = FirstValid });

        var response = await Patch(PatchEndpoint(created.Id), new { nullableValue = (object?)null });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(SqlSet, created.Id, Create(FirstValid), NullNullable);
        await AssertEntity(PgSet, created.Id, Create(FirstValid), NullNullable);
    }

    [Fact]
    public async Task Patch_UpdatesBothFieldsIndependently()
    {
        var created = await Post(CreateEndpoint, new { value = FirstValid, nullableValue = (object?)null });

        var response = await Patch(PatchEndpoint(created.Id), new { value = UpdatedValid, nullableValue = UpdatedValid });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertEntity(SqlSet, created.Id, Create(UpdatedValid), ToNullable(Create(UpdatedValid)));
        await AssertEntity(PgSet, created.Id, Create(UpdatedValid), ToNullable(Create(UpdatedValid)));
    }

    [Fact]
    public async Task Patch_NonExistentId_ReturnsNotFound()
    {
        var response = await Patch(PatchEndpoint(Guid.NewGuid()), new { value = UpdatedValid });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static void AssertJsonEquals(JsonElement element, TWire expected)
    {
        Assert.Equal(expected, element.Deserialize<TWire>());
    }
}

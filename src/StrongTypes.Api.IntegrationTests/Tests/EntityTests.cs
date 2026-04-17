using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
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
    /// <summary>Wraps a raw wire-format value in the strong type.</summary>
    protected abstract T Create(TWire raw);

    /// <summary>
    /// Seed valid value used as the "other" slot when testing one field in
    /// isolation, and as the baseline for Get/Update tests.
    /// </summary>
    protected abstract TWire FirstValid { get; }

    /// <summary>Target value for the non-nullable update test; must differ from <see cref="FirstValid"/>.</summary>
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
    public async Task NonNullable_ValidInput_PersistsInBothDatabases(TWire value)
    {
        var created = await Post(NonNullable, new { value, nullableValue = value });
        var expected = Create(value);
        await AssertEntity(SqlSet, created.Id, expected, ToNullable(expected));
        await AssertEntity(PgSet, created.Id, expected, ToNullable(expected));
    }

    [Theory]
    [MemberData(ValidInputsMember)]
    public async Task Nullable_ValidInputWithNonNullNullable_PersistsInBothDatabases(TWire value)
    {
        var created = await Post(Nullable, new { value, nullableValue = value });
        var expected = Create(value);
        await AssertEntity(SqlSet, created.Id, expected, ToNullable(expected));
        await AssertEntity(PgSet, created.Id, expected, ToNullable(expected));
    }

    [Fact]
    public async Task Nullable_ValidValueWithNullNullable_PersistsInBothDatabases()
    {
        var created = await Post(Nullable,
            new { value = (object?)FirstValid, nullableValue = (object?)null });
        await AssertEntity(SqlSet, created.Id, Create(FirstValid), NullNullable);
        await AssertEntity(PgSet, created.Id, Create(FirstValid), NullNullable);
    }

    // ── Create: invalid (non-null) ───────────────────────────────────────
    // Each invalid value is tested in both slots independently; the other
    // slot holds FirstValid so the test isolates the one field being checked.

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task NonNullable_InvalidValue_ReturnsBadRequest(TWire invalid)
    {
        var response = await Client.PostAsJsonAsync(
            NonNullable, new { value = (object?)invalid, nullableValue = (object?)FirstValid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task NonNullable_InvalidNullableValue_ReturnsBadRequest(TWire invalid)
    {
        var response = await Client.PostAsJsonAsync(
            NonNullable, new { value = (object?)FirstValid, nullableValue = (object?)invalid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task Nullable_InvalidValue_ReturnsBadRequest(TWire invalid)
    {
        var response = await Client.PostAsJsonAsync(
            Nullable, new { value = (object?)invalid, nullableValue = (object?)FirstValid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task Nullable_InvalidNullableValue_ReturnsBadRequest(TWire invalid)
    {
        var response = await Client.PostAsJsonAsync(
            Nullable, new { value = (object?)FirstValid, nullableValue = (object?)invalid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
#pragma warning restore xUnit1015

    // ── Create: null ─────────────────────────────────────────────────────
    // Value is always non-nullable, so null Value is a 400 on either endpoint.
    // NullableValue is non-nullable only on /non-nullable; on /nullable, null
    // NullableValue is the legit case (covered by the valid-null test above).

    [Fact]
    public async Task NonNullable_NullValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            NonNullable, new { value = (object?)null, nullableValue = (object?)FirstValid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonNullable_NullNullableValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            NonNullable, new { value = (object?)FirstValid, nullableValue = (object?)null }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Nullable_NullValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            Nullable, new { value = (object?)null, nullableValue = (object?)FirstValid }, Ct);
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

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
        AssertJsonEquals(sqlJson.GetProperty("value"), FirstValid);
        AssertJsonEquals(sqlJson.GetProperty("nullableValue"), FirstValid);

        var pgJson = await Get(PostgreSqlGet(entity.Id));
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

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
        AssertJsonEquals(sqlJson.GetProperty("value"), FirstValid);

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        AssertJsonEquals(pgJson.GetProperty("value"), FirstValid);
    }

    // ── Update ───────────────────────────────────────────────────────────

    [Fact]
    public async Task NonNullable_Update_PersistsNewValueAndNullableValueInBothDatabases()
    {
        var created = await Post(NonNullable, new { value = FirstValid, nullableValue = FirstValid });
        await Put(UpdateNonNullable(created.Id),
            new { value = UpdatedValid, nullableValue = UpdatedValid });

        var updated = Create(UpdatedValid);
        await AssertEntity(SqlSet, created.Id, updated, ToNullable(updated));
        await AssertEntity(PgSet, created.Id, updated, ToNullable(updated));
    }

    [Fact]
    public async Task Nullable_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var created = await Post(Nullable,
            new { value = (object?)FirstValid, nullableValue = (object?)null });
        await Put(UpdateNullable(created.Id),
            new { value = (object?)FirstValid, nullableValue = (object?)UpdatedValid });

        await AssertEntity(SqlSet, created.Id, Create(FirstValid), ToNullable(Create(UpdatedValid)));
        await AssertEntity(PgSet, created.Id, Create(FirstValid), ToNullable(Create(UpdatedValid)));
    }

    [Fact]
    public async Task Nullable_ClearsNullableValueToNullInBothDatabases()
    {
        var created = await Post(NonNullable, new { value = FirstValid, nullableValue = FirstValid });
        await Put(UpdateNullable(created.Id),
            new { value = (object?)FirstValid, nullableValue = (object?)null });

        await AssertEntity(SqlSet, created.Id, Create(FirstValid), NullNullable);
        await AssertEntity(PgSet, created.Id, Create(FirstValid), NullNullable);
    }

    // For numeric TWire (int, long, …) and string TWire, GetRawText() of a
    // JsonElement and JsonSerializer.Serialize(expected) both yield the same
    // JSON literal, so string comparison is a universal check.
    private static void AssertJsonEquals(JsonElement element, TWire expected)
    {
        Assert.Equal(JsonSerializer.Serialize(expected), element.GetRawText());
    }
}

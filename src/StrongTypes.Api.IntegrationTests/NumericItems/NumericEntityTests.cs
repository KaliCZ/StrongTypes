using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems;

/// <summary>
/// Compile-time contract every numeric entity test class must satisfy. The
/// <c>static abstract</c> members force each concrete subclass to supply the
/// <see cref="TheoryData{T}"/> lists — a missing one is a build error, not a
/// silently skipped test. xUnit resolves the actual members by reflection on
/// the concrete type at discovery time.
/// </summary>
public interface INumericTestData
{
    static abstract TheoryData<int> ValidInputs { get; }
    static abstract TheoryData<int> InvalidInputs { get; }
}

/// <summary>
/// Shared Create/Get/Update suite for every <see cref="IEntity{TSelf, T, TNullable}"/>
/// whose underlying value is an integer-backed strong type. Concrete subclasses
/// plug in <c>RoutePrefix</c>, a <c>Create(int)</c> factory, two seed values
/// (<see cref="FirstValid"/>, <see cref="UpdatedValid"/>), and the two
/// <see cref="TheoryData{T}"/> lists required by <see cref="INumericTestData"/>.
/// </summary>
public abstract class NumericEntityTests<TSelf, TEntity, T>(TestWebApplicationFactory factory)
    : IntegrationTestBase<TEntity, T, T?>(factory)
    where TSelf : NumericEntityTests<TSelf, TEntity, T>, INumericTestData
    where TEntity : class, IEntity<TEntity, T, T?>
    where T : struct
{
    /// <summary>Wraps a raw wire-format <c>int</c> in the strong type.</summary>
    protected abstract T Create(int raw);

    /// <summary>
    /// Seed valid value used as the "other" slot when testing one field in
    /// isolation, and as the baseline for Get/Update tests. Should match the
    /// first entry of <c>ValidInputs</c>.
    /// </summary>
    protected abstract int FirstValid { get; }

    /// <summary>Target value for the non-nullable update test; must differ from <see cref="FirstValid"/>.</summary>
    protected abstract int UpdatedValid { get; }

    // The analyzer can't see ValidInputs/InvalidInputs because they're static
    // abstract on INumericTestData and only materialize on the concrete
    // subclass; xUnit's runtime discovery resolves them off the concrete
    // test class via reflection.
    protected const string ValidInputsMember = nameof(INumericTestData.ValidInputs);
    protected const string InvalidInputsMember = nameof(INumericTestData.InvalidInputs);
#pragma warning disable xUnit1015

    // ── Create: valid ────────────────────────────────────────────────────

    [Theory]
    [MemberData(ValidInputsMember)]
    public async Task NonNullable_ValidInput_PersistsInBothDatabases(int value)
    {
        var created = await Post(NonNullable, Body(value, value));
        await AssertEntity(SqlSet, created.Id, Create(value), Create(value));
        await AssertEntity(PgSet, created.Id, Create(value), Create(value));
    }

    [Theory]
    [MemberData(ValidInputsMember)]
    public async Task Nullable_ValidInputWithNonNullNullable_PersistsInBothDatabases(int value)
    {
        var created = await Post(Nullable, Body(value, (int?)value));
        await AssertEntity(SqlSet, created.Id, Create(value), Create(value));
        await AssertEntity(PgSet, created.Id, Create(value), Create(value));
    }

    [Fact]
    public async Task Nullable_ValidValueWithNullNullable_PersistsInBothDatabases()
    {
        var created = await Post(Nullable, Body(FirstValid, (int?)null));
        await AssertEntity(SqlSet, created.Id, Create(FirstValid), null);
        await AssertEntity(PgSet, created.Id, Create(FirstValid), null);
    }

    // ── Create: invalid number (non-null) ────────────────────────────────
    // Each invalid number is tested in both slots independently; the other
    // slot holds FirstValid so the test isolates the one field being checked.

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task NonNullable_InvalidValue_ReturnsBadRequest(int invalid)
    {
        var response = await Client.PostAsJsonAsync(
            NonNullable, new { value = invalid, nullableValue = FirstValid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task NonNullable_InvalidNullableValue_ReturnsBadRequest(int invalid)
    {
        var response = await Client.PostAsJsonAsync(
            NonNullable, new { value = FirstValid, nullableValue = invalid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task Nullable_InvalidValue_ReturnsBadRequest(int invalid)
    {
        var response = await Client.PostAsJsonAsync(
            Nullable, new { value = invalid, nullableValue = (int?)FirstValid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task Nullable_InvalidNullableValue_ReturnsBadRequest(int invalid)
    {
        var response = await Client.PostAsJsonAsync(
            Nullable, new { value = FirstValid, nullableValue = (int?)invalid }, Ct);
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
            NonNullable, new { value = (int?)null, nullableValue = (int?)FirstValid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NonNullable_NullNullableValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            NonNullable, new { value = (int?)FirstValid, nullableValue = (int?)null }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Nullable_NullValue_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync(
            Nullable, new { value = (int?)null, nullableValue = (int?)FirstValid }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Get ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsEntityWithCamelCaseJsonFromBothDatabases()
    {
        var entity = TEntity.Create(Create(FirstValid), Create(FirstValid));
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
        Assert.Equal(FirstValid, sqlJson.GetProperty("value").GetInt32());
        Assert.Equal(FirstValid, sqlJson.GetProperty("nullableValue").GetInt32());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(entity.Id, pgJson.GetProperty("id").GetGuid());
        Assert.Equal(FirstValid, pgJson.GetProperty("value").GetInt32());
        Assert.Equal(FirstValid, pgJson.GetProperty("nullableValue").GetInt32());
    }

    [Fact]
    public async Task Get_SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var entity = TEntity.Create(Create(FirstValid), null);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal(FirstValid, sqlJson.GetProperty("value").GetInt32());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal(FirstValid, pgJson.GetProperty("value").GetInt32());
    }

    // ── Update ───────────────────────────────────────────────────────────

    [Fact]
    public async Task NonNullable_Update_PersistsNewValueAndNullableValueInBothDatabases()
    {
        var created = await Post(NonNullable, Body(FirstValid, FirstValid));
        await Put(UpdateNonNullable(created.Id), Body(UpdatedValid, UpdatedValid));

        await AssertEntity(SqlSet, created.Id, Create(UpdatedValid), Create(UpdatedValid));
        await AssertEntity(PgSet, created.Id, Create(UpdatedValid), Create(UpdatedValid));
    }

    [Fact]
    public async Task Nullable_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var created = await Post(Nullable, Body(FirstValid, (int?)null));
        await Put(UpdateNullable(created.Id), Body(FirstValid, (int?)UpdatedValid));

        await AssertEntity(SqlSet, created.Id, Create(FirstValid), Create(UpdatedValid));
        await AssertEntity(PgSet, created.Id, Create(FirstValid), Create(UpdatedValid));
    }

    [Fact]
    public async Task Nullable_ClearsNullableValueToNullInBothDatabases()
    {
        var created = await Post(NonNullable, Body(FirstValid, FirstValid));
        await Put(UpdateNullable(created.Id), Body(FirstValid, (int?)null));

        await AssertEntity(SqlSet, created.Id, Create(FirstValid), null);
        await AssertEntity(PgSet, created.Id, Create(FirstValid), null);
    }
}

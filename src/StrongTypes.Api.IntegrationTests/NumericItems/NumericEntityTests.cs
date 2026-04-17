using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems;

/// <summary>
/// Shared Create/Get/Update suite for every <see cref="IEntity{TSelf, T, TNullable}"/>
/// whose underlying value is an integer-backed strong type. Concrete subclasses
/// plug in <see cref="RoutePrefix"/>, a <c>Create(int)</c> factory, two seed
/// pairs (<see cref="SeedValid"/>, <see cref="SeedValidUpdate"/>), and two
/// static <see cref="Xunit.TheoryData{T1, T2}"/> members named
/// <c>ValidInputs</c> and <c>InvalidInputs</c> that xUnit discovers by name
/// on the concrete test class.
/// </summary>
public abstract class NumericEntityTests<TEntity, T>(TestWebApplicationFactory factory)
    : IntegrationTestBase<TEntity, T, T?>(factory)
    where TEntity : class, IEntity<TEntity, T, T?>
    where T : struct
{
    /// <summary>Wraps a raw wire-format <c>int</c> in the strong type.</summary>
    protected abstract T Create(int raw);

    /// <summary>(value, nullableValue) pair used by [Fact] Get/Update tests.</summary>
    protected abstract (int value, int nullableValue) SeedValid { get; }

    /// <summary>Target state for the non-nullable update test.</summary>
    protected abstract (int value, int nullableValue) SeedValidUpdate { get; }

    // ── Create ───────────────────────────────────────────────────────────

    // The analyzer can't see the ValidInputs/InvalidInputs members because
    // they're declared on the concrete subclass; xUnit's runtime discovery
    // still resolves them off the concrete test class via reflection.
#pragma warning disable xUnit1015
    [Theory]
    [MemberData(ValidInputsMember)]
    public async Task NonNullable_ValidInputs_PersistsInBothDatabases(int value, int nullableValue)
    {
        var created = await Post(NonNullable, Body(value, nullableValue));
        await AssertEntity(SqlSet, created.Id, Create(value), Create(nullableValue));
        await AssertEntity(PgSet, created.Id, Create(value), Create(nullableValue));
    }

    [Theory]
    [MemberData(ValidInputsMember)]
    public async Task Nullable_ValidInputsWithNonNullNullable_PersistsInBothDatabases(int value, int nullableValue)
    {
        var created = await Post(Nullable, Body(value, (int?)nullableValue));
        await AssertEntity(SqlSet, created.Id, Create(value), Create(nullableValue));
        await AssertEntity(PgSet, created.Id, Create(value), Create(nullableValue));
    }

    [Fact]
    public async Task Nullable_ValidValueWithNullNullable_PersistsInBothDatabases()
    {
        var (val, _) = SeedValid;
        var created = await Post(Nullable, Body(val, (int?)null));
        await AssertEntity(SqlSet, created.Id, Create(val), null);
        await AssertEntity(PgSet, created.Id, Create(val), null);
    }

    // InvalidInputs covers both endpoints: every pair is bad regardless of
    // whether NullableValue is allowed to be null — i.e. the Value itself is
    // invalid or null, or NullableValue is a non-null invalid number.
    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task NonNullable_InvalidInputs_ReturnsBadRequest(int? value, int? nullableValue)
    {
        var response = await Client.PostAsJsonAsync(NonNullable, new { value, nullableValue }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task Nullable_InvalidInputs_ReturnsBadRequest(int? value, int? nullableValue)
    {
        var response = await Client.PostAsJsonAsync(Nullable, new { value, nullableValue }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
#pragma warning restore xUnit1015

    // Endpoint-specific: null NullableValue with a valid Value is bad on the
    // non-nullable endpoint but valid on the nullable endpoint (covered above).
    [Fact]
    public async Task NonNullable_NullNullableValueWithValidValue_ReturnsBadRequest()
    {
        var (val, _) = SeedValid;
        var response = await Client.PostAsJsonAsync(
            NonNullable,
            new { value = (int?)val, nullableValue = (int?)null },
            Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Get ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_ReturnsEntityWithCamelCaseJsonFromBothDatabases()
    {
        var (val, nullable) = SeedValid;
        var entity = TEntity.Create(Create(val), Create(nullable));
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(entity.Id, sqlJson.GetProperty("id").GetGuid());
        Assert.Equal(val, sqlJson.GetProperty("value").GetInt32());
        Assert.Equal(nullable, sqlJson.GetProperty("nullableValue").GetInt32());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(entity.Id, pgJson.GetProperty("id").GetGuid());
        Assert.Equal(val, pgJson.GetProperty("value").GetInt32());
        Assert.Equal(nullable, pgJson.GetProperty("nullableValue").GetInt32());
    }

    [Fact]
    public async Task Get_SerializesNullNullableValueAsJsonNullFromBothDatabases()
    {
        var (val, _) = SeedValid;
        var entity = TEntity.Create(Create(val), null);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);

        var sqlJson = await Get(SqlServerGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, sqlJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal(val, sqlJson.GetProperty("value").GetInt32());

        var pgJson = await Get(PostgreSqlGet(entity.Id));
        Assert.Equal(JsonValueKind.Null, pgJson.GetProperty("nullableValue").ValueKind);
        Assert.Equal(val, pgJson.GetProperty("value").GetInt32());
    }

    // ── Update ───────────────────────────────────────────────────────────

    [Fact]
    public async Task NonNullable_Update_PersistsNewValueAndNullableValueInBothDatabases()
    {
        var (val, nullable) = SeedValid;
        var (newVal, newNullable) = SeedValidUpdate;

        var created = await Post(NonNullable, Body(val, nullable));
        await Put(UpdateNonNullable(created.Id), Body(newVal, newNullable));

        await AssertEntity(SqlSet, created.Id, Create(newVal), Create(newNullable));
        await AssertEntity(PgSet, created.Id, Create(newVal), Create(newNullable));
    }

    [Fact]
    public async Task Nullable_SetsNullableValueFromNullToValueInBothDatabases()
    {
        var (val, nullable) = SeedValid;
        var created = await Post(Nullable, Body(val, (int?)null));
        await Put(UpdateNullable(created.Id), Body(val, (int?)nullable));

        await AssertEntity(SqlSet, created.Id, Create(val), Create(nullable));
        await AssertEntity(PgSet, created.Id, Create(val), Create(nullable));
    }

    [Fact]
    public async Task Nullable_ClearsNullableValueToNullInBothDatabases()
    {
        var (val, nullable) = SeedValid;
        var created = await Post(NonNullable, Body(val, nullable));
        await Put(UpdateNullable(created.Id), Body(val, (int?)null));

        await AssertEntity(SqlSet, created.Id, Create(val), null);
        await AssertEntity(PgSet, created.Id, Create(val), null);
    }

    // xUnit discovers MemberData via reflection on the concrete test class;
    // these names must match public static TheoryData members on the subclass.
    protected const string ValidInputsMember = "ValidInputs";
    protected const string InvalidInputsMember = "InvalidInputs";
}

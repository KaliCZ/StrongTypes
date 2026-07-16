using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// The read-validation guarantee: a stored row violating the wrapper's invariant (planted via
/// raw SQL — the strong types cannot produce one) throws on materialization, in both
/// persistence shapes. Variant-specific invariants live on the concrete subclasses.
/// </summary>
public abstract class IntervalReadValidationTestsBase<TColumnsEntity, TJsonEntity, TInterval>(TestWebApplicationFactory factory)
    : IntegrationTestBase<TColumnsEntity, TInterval, TInterval?>(factory)
    where TColumnsEntity : class, IEntity<TColumnsEntity, TInterval, TInterval?>
    where TJsonEntity : class, IEntity<TJsonEntity, TInterval, TInterval?>
    where TInterval : struct
{
    public static TheoryData<string> Providers => new() { "sql-server", "postgresql" };

    /// <summary>A valid interval with <c>Start = 1</c>, <c>End = 10</c>; reversing its endpoints to <c>Start = 10</c>, <c>End = 1</c> is the violation the corruption tests plant.</summary>
    protected abstract TInterval Valid { get; }

    [Theory, MemberData(nameof(Providers))]
    public async Task CorruptedEndpointColumns_ThrowOnRead(string provider)
    {
        SkipIfSqlServerUnavailable(provider);
        var db = provider == "sql-server" ? SqlDb : (DbContext)PgDb;

        var entity = TColumnsEntity.Create(Valid, null);
        db.Add(entity);
        await db.SaveChangesAsync(Ct);
        db.ChangeTracker.Clear();

        var complexType = ComplexType<TColumnsEntity>(db, nameof(entity.Value));
        var start = ColumnName(db, complexType, nameof(FiniteInterval<int>.Start), provider);
        var end = ColumnName(db, complexType, nameof(FiniteInterval<int>.End), provider);
        await CorruptRow<TColumnsEntity>(db, provider, entity.Id, $"{start} = 10, {end} = 1");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Set<TColumnsEntity>().SingleAsync(e => e.Id == entity.Id, Ct));
        Assert.Contains("Start <= End", exception.Message);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task CorruptedJsonColumn_ReversedEndpoints_ThrowsOnRead(string provider)
    {
        var exception = await ReadCorruptedJson(provider, """{"Start":10,"End":1}""");
        Assert.IsType<JsonException>(exception);
        Assert.Contains("less than or equal to", exception.Message);
    }

    /// <summary>Plants <paramref name="corruptedJson"/> into the JSON value column, reads the row back, and returns the base exception thrown while materializing it.</summary>
    protected async Task<Exception> ReadCorruptedJson(string provider, string corruptedJson)
    {
        SkipIfSqlServerUnavailable(provider);
        var db = provider == "sql-server" ? SqlDb : (DbContext)PgDb;

        var entity = TJsonEntity.Create(Valid, null);
        db.Add(entity);
        await db.SaveChangesAsync(Ct);
        db.ChangeTracker.Clear();

        var entityType = db.Model.FindEntityType(typeof(TJsonEntity))!;
        var valueColumn = Quote(entityType.FindProperty(nameof(entity.Value))!.GetColumnName(), provider);
        // ExecuteSqlRaw runs the SQL through composite formatting, so brace-escape the JSON literal.
        var literal = corruptedJson.Replace("{", "{{").Replace("}", "}}");
        await CorruptRow<TJsonEntity>(db, provider, entity.Id, $"{valueColumn} = '{literal}'");

        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => db.Set<TJsonEntity>().SingleAsync(e => e.Id == entity.Id, Ct));
        return exception.GetBaseException();
    }

    private static IComplexType ComplexType<TEntity>(DbContext db, string propertyName) =>
        db.Model.FindEntityType(typeof(TEntity))!.GetComplexProperties().Single(p => p.Name == propertyName).ComplexType;

    private static string ColumnName(DbContext db, IComplexType complexType, string endpointName, string provider)
    {
        var entityType = db.Model.FindEntityType(complexType.ContainingEntityType.ClrType)!;
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        return Quote(complexType.GetProperties().Single(p => p.Name == endpointName).GetColumnName(storeObject)!, provider);
    }

    private static string Quote(string identifier, string provider) =>
        provider == "sql-server" ? $"[{identifier}]" : $"\"{identifier}\"";

    private Task CorruptRow<TEntity>(DbContext db, string provider, Guid id, string setClause) where TEntity : class
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity))!;
        var table = Quote(entityType.GetTableName()!, provider);
        var idColumn = Quote(entityType.FindPrimaryKey()!.Properties.Single().GetColumnName(), provider);
        // Identifiers come from EF metadata and the id is a Guid — nothing user-supplied to inject.
        var sql = $"UPDATE {table} SET {setClause} WHERE {idColumn} = '{id}'";
#pragma warning disable EF1002
        return db.Database.ExecuteSqlRawAsync(sql, Ct);
#pragma warning restore EF1002
    }
}

// ── FiniteInterval<int> ────────────────────────────────────────────────────

[Collection(IntegrationTestCollection.Name)]
public sealed class FiniteIntervalReadValidationTests(TestWebApplicationFactory factory)
    : IntervalReadValidationTestsBase<FiniteIntervalColumnsEntity, FiniteIntervalEntity, FiniteInterval<int>>(factory)
{
    protected override FiniteInterval<int> Valid => FiniteInterval.Create(1, 10);

    [Theory, MemberData(nameof(Providers))]
    public async Task CorruptedJsonColumn_MissingStart_ThrowsOnRead(string provider)
    {
        var exception = await ReadCorruptedJson(provider, """{"End":10}""");
        Assert.IsType<JsonException>(exception);
        Assert.Contains("requires the 'Start' property", exception.Message);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task CorruptedJsonColumn_MissingEnd_ThrowsOnRead(string provider)
    {
        var exception = await ReadCorruptedJson(provider, """{"Start":1}""");
        Assert.IsType<JsonException>(exception);
        Assert.Contains("requires the 'End' property", exception.Message);
    }
}

// ── Interval<int> ──────────────────────────────────────────────────────────

[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalReadValidationTests(TestWebApplicationFactory factory)
    : IntervalReadValidationTestsBase<IntervalColumnsEntity, IntervalEntity, Interval<int>>(factory)
{
    // Both endpoints are optional, so the only read-time invariant is Start <= End,
    // covered by the shared reversed-endpoint tests.
    protected override Interval<int> Valid => Interval.Create(1, 10);
}

// ── IntervalFrom<int> ──────────────────────────────────────────────────────

[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalFromReadValidationTests(TestWebApplicationFactory factory)
    : IntervalReadValidationTestsBase<IntervalFromColumnsEntity, IntervalFromEntity, IntervalFrom<int>>(factory)
{
    protected override IntervalFrom<int> Valid => IntervalFrom.Create(1, 10);

    [Theory, MemberData(nameof(Providers))]
    public async Task CorruptedJsonColumn_MissingRequiredStart_ThrowsOnRead(string provider)
    {
        var exception = await ReadCorruptedJson(provider, """{"End":10}""");
        Assert.IsType<JsonException>(exception);
        Assert.Contains("requires the 'Start' property", exception.Message);
    }
}

// ── IntervalUntil<int> ─────────────────────────────────────────────────────

[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalUntilReadValidationTests(TestWebApplicationFactory factory)
    : IntervalReadValidationTestsBase<IntervalUntilColumnsEntity, IntervalUntilEntity, IntervalUntil<int>>(factory)
{
    protected override IntervalUntil<int> Valid => IntervalUntil.Create(1, 10);

    [Theory, MemberData(nameof(Providers))]
    public async Task CorruptedJsonColumn_MissingRequiredEnd_ThrowsOnRead(string provider)
    {
        var exception = await ReadCorruptedJson(provider, """{"Start":1}""");
        Assert.IsType<JsonException>(exception);
        Assert.Contains("requires the 'End' property", exception.Message);
    }
}

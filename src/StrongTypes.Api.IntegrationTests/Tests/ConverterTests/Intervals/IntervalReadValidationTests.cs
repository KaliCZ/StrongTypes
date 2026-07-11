using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies the read-validation guarantee: a stored row violating
/// <c>Start &lt;= End</c> (planted via raw SQL, bypassing the strong types)
/// throws on materialization instead of producing an invalid interval — in
/// both persistence shapes, on both providers.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalReadValidationTests(TestWebApplicationFactory factory)
    : IntegrationTestBase<FiniteIntervalColumnsEntity, FiniteInterval<int>, FiniteInterval<int>?>(factory)
{
    public static TheoryData<string> Providers => new() { "sql-server", "postgresql" };

    [Theory, MemberData(nameof(Providers))]
    public async Task CorruptedEndpointColumns_ThrowOnRead(string provider)
    {
        SkipIfSqlServerUnavailable(provider);
        var db = provider == "sql-server" ? SqlDb : (DbContext)PgDb;

        var entity = FiniteIntervalColumnsEntity.Create(FiniteInterval.Create(1, 10), null);
        db.Add(entity);
        await db.SaveChangesAsync(Ct);
        db.ChangeTracker.Clear();

        var complexType = ComplexType<FiniteIntervalColumnsEntity>(db, nameof(entity.Value));
        var start = ColumnName(db, complexType, nameof(FiniteInterval<int>.Start), provider);
        var end = ColumnName(db, complexType, nameof(FiniteInterval<int>.End), provider);
        await CorruptRow<FiniteIntervalColumnsEntity>(db, provider, entity.Id, $"{start} = 10, {end} = 1");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => db.Set<FiniteIntervalColumnsEntity>().SingleAsync(e => e.Id == entity.Id, Ct));
        Assert.Contains("Start <= End", exception.Message);
    }

    [Theory, MemberData(nameof(Providers))]
    public async Task CorruptedJsonColumn_ThrowsOnRead(string provider)
    {
        SkipIfSqlServerUnavailable(provider);
        var db = provider == "sql-server" ? SqlDb : (DbContext)PgDb;

        var entity = FiniteIntervalEntity.Create(FiniteInterval.Create(1, 10), null);
        db.Add(entity);
        await db.SaveChangesAsync(Ct);
        db.ChangeTracker.Clear();

        var entityType = db.Model.FindEntityType(typeof(FiniteIntervalEntity))!;
        var valueColumn = Quote(entityType.FindProperty(nameof(entity.Value))!.GetColumnName(), provider);
        // Doubled braces: ExecuteSqlRaw runs the SQL through composite formatting.
        await CorruptRow<FiniteIntervalEntity>(db, provider, entity.Id, valueColumn + """ = '{{"Start":10,"End":1}}'""");

        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => db.Set<FiniteIntervalEntity>().SingleAsync(e => e.Id == entity.Id, Ct));
        Assert.IsType<JsonException>(exception.GetBaseException());
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
        var idColumn = Quote(entityType.FindProperty(nameof(FiniteIntervalColumnsEntity.Id))!.GetColumnName(), provider);
        // Identifiers come from EF metadata and the id is a Guid — nothing user-supplied to inject.
        var sql = $"UPDATE {table} SET {setClause} WHERE {idColumn} = '{id}'";
#pragma warning disable EF1002
        return db.Database.ExecuteSqlRawAsync(sql, Ct);
#pragma warning restore EF1002
    }
}

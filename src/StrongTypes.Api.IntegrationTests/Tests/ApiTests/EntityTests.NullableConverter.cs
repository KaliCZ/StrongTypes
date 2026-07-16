using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using StrongTypes.Api.Entities;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>EF Core value-converter coverage for the nullable slot; on the shared base so every nullable strong type runs it.</summary>
public abstract partial class EntityTests<TSelf, TEntity, T, TNullable, TWire>
{
    /// <summary>
    /// Orders values the way the database orders the stored column; override when the CLR default
    /// differs — e.g. <c>MailAddress</c>, not <see cref="IComparable"/> and stored as its address string.
    /// </summary>
    protected virtual IComparer<T> ValueComparer => Comparer<T>.Default;

    [Fact]
    public async Task NullableValue_RoundTripsNonNullAndNullThroughConverter()
    {
        var withValue = await SeedAsync(FirstValid, ToNullable(Create(FirstValid)));
        var withNull = await SeedAsync(FirstValid, NullNullable);

        await ForEachProvider(async set =>
        {
            var populated = await set.AsNoTracking().SingleAsync(e => e.Id == withValue, Ct);
            Assert.Equal(ToNullable(Create(FirstValid)), populated.NullableValue);

            var empty = await set.AsNoTracking().SingleAsync(e => e.Id == withNull, Ct);
            Assert.Null(empty.NullableValue);
        });
    }

    [Fact]
    public async Task NullableValue_FilterByNullAndNotNull_TranslatesToSql()
    {
        var withValue = await SeedAsync(FirstValid, ToNullable(Create(FirstValid)));
        var withNull = await SeedAsync(FirstValid, NullNullable);
        Guid[] seeded = [withValue, withNull];

        await ForEachProvider(async set =>
        {
            var nulls = await set.Where(e => seeded.Contains(e.Id))
                .Where(NullableValueMatches(NullNullable, equal: true))
                .Select(e => e.Id).ToListAsync(Ct);
            Assert.Contains(withNull, nulls);
            Assert.DoesNotContain(withValue, nulls);

            var nonNulls = await set.Where(e => seeded.Contains(e.Id))
                .Where(NullableValueMatches(NullNullable, equal: false))
                .Select(e => e.Id).ToListAsync(Ct);
            Assert.Contains(withValue, nonNulls);
            Assert.DoesNotContain(withNull, nonNulls);
        });
    }

    [Fact]
    public async Task NullableValue_FilterByValue_TranslatesToSql()
    {
        var match = await SeedAsync(FirstValid, ToNullable(Create(FirstValid)));
        var other = await SeedAsync(FirstValid, ToNullable(Create(UpdatedValid)));
        Guid[] seeded = [match, other];
        var needle = ToNullable(Create(FirstValid));

        await ForEachProvider(async set =>
        {
            var ids = await set.Where(e => seeded.Contains(e.Id))
                .Where(NullableValueMatches(needle, equal: true))
                .Select(e => e.Id).ToListAsync(Ct);
            Assert.Contains(match, ids);
            Assert.DoesNotContain(other, ids);
        });
    }

    [Fact]
    public async Task NullableValue_OrderBy_TranslatesToSql()
    {
        var first = Create(FirstValid);
        var second = Create(UpdatedValid);
        var a = await SeedAsync(FirstValid, ToNullable(first));
        var b = await SeedAsync(FirstValid, ToNullable(second));
        Guid[] seeded = [a, b];
        Guid[] expected = ValueComparer.Compare(first, second) <= 0 ? [a, b] : [b, a];

        await ForEachProvider(async set =>
        {
            var ordered = await set.Where(e => seeded.Contains(e.Id))
                .OrderBy(NullableValueSelector())
                .Select(e => e.Id).ToListAsync(Ct);
            Assert.Equal(expected, ordered);
        });
    }

    // The converter runs Create on read, so an invariant-violating stored value
    // fails fast instead of materialising a broken wrapper.
#pragma warning disable xUnit1015
    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task NullableValue_ReadingInvariantViolatingStoredValue_Throws(TWire invalidStored)
    {
        var id = Guid.NewGuid();
        await RawInsertAsync(PgDb, id, FirstValid, invalidStored);
        if (SqlServerAvailable)
        {
            await RawInsertAsync(SqlDb, id, FirstValid, invalidStored);
        }

        await ForEachProvider(async set =>
            await Assert.ThrowsAnyAsync<Exception>(
                async () => await set.AsNoTracking().SingleAsync(e => e.Id == id, Ct)));
    }
#pragma warning restore xUnit1015

    private async Task<Guid> SeedAsync(TWire value, TNullable nullableValue)
    {
        var entity = TEntity.Create(Create(value), nullableValue);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SqlDb.SaveChangesAsync(Ct);
        await PgDb.SaveChangesAsync(Ct);
        // Detach so the reads below materialise from the database (running the
        // converter's read path) rather than returning the tracked instance.
        SqlDb.ChangeTracker.Clear();
        PgDb.ChangeTracker.Clear();
        return entity.Id;
    }

    private async Task ForEachProvider(Func<DbSet<TEntity>, Task> assert)
    {
        await assert(PgSet);
        if (SqlServerAvailable)
        {
            await assert(SqlSet);
        }
    }

    private static Expression<Func<TEntity, TNullable>> NullableValueSelector()
    {
        var e = Expression.Parameter(typeof(TEntity), "e");
        var property = Expression.Property(e, nameof(IEntity<TEntity, T, TNullable>.NullableValue));
        return Expression.Lambda<Func<TEntity, TNullable>>(property, e);
    }

    private static Expression<Func<TEntity, bool>> NullableValueMatches(TNullable value, bool equal)
    {
        var e = Expression.Parameter(typeof(TEntity), "e");
        var property = Expression.Property(e, nameof(IEntity<TEntity, T, TNullable>.NullableValue));
        var constant = Expression.Constant(value, typeof(TNullable));
        var body = equal ? Expression.Equal(property, constant) : Expression.NotEqual(property, constant);
        return Expression.Lambda<Func<TEntity, bool>>(body, e);
    }

    private async Task RawInsertAsync(DbContext ctx, Guid id, TWire value, TWire nullableValue)
    {
        var sqlHelper = ctx.GetService<ISqlGenerationHelper>();
        var entityType = ctx.Model.FindEntityType(typeof(TEntity))!;
        var table = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)!.Value;
        string Column(string member) => sqlHelper.DelimitIdentifier(entityType.FindProperty(member)!.GetColumnName(table)!);

        var insert =
            $"INSERT INTO {sqlHelper.DelimitIdentifier(entityType.GetTableName()!, entityType.GetSchema())} " +
            $"({Column(nameof(IEntity<TEntity, T, TNullable>.Id))}, " +
            $"{Column(nameof(IEntity<TEntity, T, TNullable>.Value))}, " +
            $"{Column(nameof(IEntity<TEntity, T, TNullable>.NullableValue))}) VALUES ({{0}}, {{1}}, {{2}})";

        await ctx.Database.ExecuteSqlRawAsync(insert, [id, value!, nullableValue!], Ct);
    }
}

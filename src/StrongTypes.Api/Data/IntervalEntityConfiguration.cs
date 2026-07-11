using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.EfCore;

namespace StrongTypes.Api.Data;

/// <summary>Maps every interval-mapping configuration under test, identically for both DbContexts: the <c>*IntervalEntity</c> set opts into the single JSON column; the explicit-column entities pin each <see cref="IntervalBoundMode"/> entry point (no-arg default, both <c>AlwaysInclusive</c>, both <c>AlwaysExclusive</c>, per-value <c>Stored</c>, end-only <c>AlwaysExclusive</c>). The parallel <c>*ColumnsEntity</c> set stays unconfigured on purpose — it exercises the <c>UseStrongTypes()</c> two-endpoint-column default.</summary>
internal static class IntervalEntityConfiguration
{
    public static void ConfigureIntervalEntities(this ModelBuilder modelBuilder)
    {
        Configure<FiniteIntervalEntity, FiniteInterval<int>>(modelBuilder);
        Configure<IntervalEntity, Interval<int>>(modelBuilder);
        Configure<IntervalFromEntity, IntervalFrom<int>>(modelBuilder);
        Configure<IntervalUntilEntity, IntervalUntil<int>>(modelBuilder);

        modelBuilder.Entity<StoredBoundsIntervalEntity>()
            .HasIntervalColumns(e => e.Value, startBound: IntervalBoundMode.Stored, endBound: IntervalBoundMode.Stored);
        modelBuilder.Entity<StoredBoundsIntervalEntity>()
            .HasIntervalColumns(e => e.NullableValue, startBound: IntervalBoundMode.Stored, endBound: IntervalBoundMode.Stored);
        modelBuilder.Entity<ExclusiveEndIntervalEntity>()
            .HasIntervalColumns(e => e.Value, endBound: IntervalBoundMode.AlwaysExclusive);

        modelBuilder.Entity<ExplicitColumnsIntervalEntity>().HasIntervalColumns(e => e.Value);
        modelBuilder.Entity<ExplicitColumnsIntervalEntity>().HasIntervalColumns(e => e.NullableValue);

        modelBuilder.Entity<AlwaysInclusiveColumnsIntervalEntity>()
            .HasIntervalColumns(e => e.Value, IntervalBoundMode.AlwaysInclusive, IntervalBoundMode.AlwaysInclusive);
        modelBuilder.Entity<AlwaysInclusiveColumnsIntervalEntity>()
            .HasIntervalColumns(e => e.NullableValue, IntervalBoundMode.AlwaysInclusive, IntervalBoundMode.AlwaysInclusive);

        modelBuilder.Entity<AlwaysExclusiveColumnsIntervalEntity>()
            .HasIntervalColumns(e => e.Value, IntervalBoundMode.AlwaysExclusive, IntervalBoundMode.AlwaysExclusive);
        modelBuilder.Entity<AlwaysExclusiveColumnsIntervalEntity>()
            .HasIntervalColumns(e => e.NullableValue, IntervalBoundMode.AlwaysExclusive, IntervalBoundMode.AlwaysExclusive);
    }

    private static void Configure<TEntity, TInterval>(ModelBuilder modelBuilder)
        where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
        where TInterval : struct
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.HasIntervalJsonConversion(e => e.Value);
        entity.Property(e => e.NullableValue).HasIntervalJsonConversion();
    }
}

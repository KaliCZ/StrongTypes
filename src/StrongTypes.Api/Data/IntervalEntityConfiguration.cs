using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.EfCore;

namespace StrongTypes.Api.Data;

/// <summary>Maps every interval-mapping configuration under test, identically for both DbContexts: the <c>*IntervalEntity</c> set opts into the single JSON column; the explicit-column entities pin each <see cref="IntervalBoundMode"/> entry point (no-arg default, both <c>AlwaysInclusive</c>, both <c>AlwaysExclusive</c>) across all four interval variants, plus per-value <c>Stored</c> and end-only <c>AlwaysExclusive</c> on <see cref="FiniteInterval{T}"/>. The parallel <c>*ColumnsEntity</c> set stays unconfigured on purpose — it exercises the <c>UseStrongTypes()</c> two-endpoint-column default.</summary>
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

        ConfigureDefaultColumns<ExplicitColumnsFiniteIntervalEntity, FiniteInterval<int>>(modelBuilder);
        ConfigureDefaultColumns<ExplicitColumnsIntervalFromEntity, IntervalFrom<int>>(modelBuilder);
        ConfigureDefaultColumns<ExplicitColumnsIntervalUntilEntity, IntervalUntil<int>>(modelBuilder);
        ConfigureDefaultColumns<ExplicitColumnsIntervalEntity, Interval<int>>(modelBuilder);

        ConfigureColumns<AlwaysInclusiveColumnsFiniteIntervalEntity, FiniteInterval<int>>(modelBuilder, IntervalBoundMode.AlwaysInclusive);
        ConfigureColumns<AlwaysInclusiveColumnsIntervalFromEntity, IntervalFrom<int>>(modelBuilder, IntervalBoundMode.AlwaysInclusive);
        ConfigureColumns<AlwaysInclusiveColumnsIntervalUntilEntity, IntervalUntil<int>>(modelBuilder, IntervalBoundMode.AlwaysInclusive);
        ConfigureColumns<AlwaysInclusiveColumnsIntervalEntity, Interval<int>>(modelBuilder, IntervalBoundMode.AlwaysInclusive);

        ConfigureColumns<AlwaysExclusiveColumnsFiniteIntervalEntity, FiniteInterval<int>>(modelBuilder, IntervalBoundMode.AlwaysExclusive);
        ConfigureColumns<AlwaysExclusiveColumnsIntervalFromEntity, IntervalFrom<int>>(modelBuilder, IntervalBoundMode.AlwaysExclusive);
        ConfigureColumns<AlwaysExclusiveColumnsIntervalUntilEntity, IntervalUntil<int>>(modelBuilder, IntervalBoundMode.AlwaysExclusive);
        ConfigureColumns<AlwaysExclusiveColumnsIntervalEntity, Interval<int>>(modelBuilder, IntervalBoundMode.AlwaysExclusive);
    }

    // No-argument HasIntervalColumns: the two-column shape with the default (AlwaysInclusive) bounds.
    private static void ConfigureDefaultColumns<TEntity, TInterval>(ModelBuilder modelBuilder)
        where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
        where TInterval : struct
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.HasIntervalColumns<TEntity, TInterval>(e => e.Value);
        entity.HasIntervalColumns<TEntity, TInterval>(e => e.NullableValue);
    }

    private static void ConfigureColumns<TEntity, TInterval>(ModelBuilder modelBuilder, IntervalBoundMode bounds)
        where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
        where TInterval : struct
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.HasIntervalColumns<TEntity, TInterval>(e => e.Value, bounds, bounds);
        entity.HasIntervalColumns<TEntity, TInterval>(e => e.NullableValue, bounds, bounds);
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

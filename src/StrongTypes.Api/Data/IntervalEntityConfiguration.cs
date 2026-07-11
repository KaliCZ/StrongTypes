using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.EfCore;

namespace StrongTypes.Api.Data;

/// <summary>Opts the interval entities into the single-JSON-column shape and maps the bound-mode entities' non-default <see cref="IntervalBoundMode"/>s, identically for both DbContexts. The parallel <c>*ColumnsEntity</c> set stays unconfigured on purpose — it exercises the <c>UseStrongTypes()</c> two-endpoint-column default.</summary>
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

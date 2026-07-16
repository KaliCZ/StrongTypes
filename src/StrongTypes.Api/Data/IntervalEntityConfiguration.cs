using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.EfCore;

namespace StrongTypes.Api.Data;

/// <summary>
/// Applied identically by both DbContexts. The <c>*ColumnsEntity</c> set is deliberately not
/// configured here — it exercises the <c>UseStrongTypes()</c> two-endpoint-column default.
/// </summary>
internal static class IntervalEntityConfiguration
{
    public static void ConfigureIntervalEntities(this ModelBuilder modelBuilder)
    {
        Configure<FiniteIntervalEntity, FiniteInterval<int>>(modelBuilder);
        Configure<IntervalEntity, Interval<int>>(modelBuilder);
        Configure<IntervalFromEntity, IntervalFrom<int>>(modelBuilder);
        Configure<IntervalUntilEntity, IntervalUntil<int>>(modelBuilder);

        ConfigureColumns<ExplicitColumnsFiniteIntervalEntity, FiniteInterval<int>>(modelBuilder);
        ConfigureColumns<ExplicitColumnsIntervalFromEntity, IntervalFrom<int>>(modelBuilder);
        ConfigureColumns<ExplicitColumnsIntervalUntilEntity, IntervalUntil<int>>(modelBuilder);
        ConfigureColumns<ExplicitColumnsIntervalEntity, Interval<int>>(modelBuilder);
    }

    private static void ConfigureColumns<TEntity, TInterval>(ModelBuilder modelBuilder)
        where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
        where TInterval : struct
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.HasIntervalColumns<TEntity, TInterval>(e => e.Value);
        entity.HasIntervalColumns<TEntity, TInterval>(e => e.NullableValue);
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

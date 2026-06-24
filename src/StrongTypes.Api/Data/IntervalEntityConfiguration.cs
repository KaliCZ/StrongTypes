using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.EfCore;

namespace StrongTypes.Api.Data;

/// <summary>
/// Maps every interval entity to the single-JSON-column persistence shape, the
/// only shape that round-trips the nullable <c>NullableValue</c> slot. The
/// required <c>Value</c> uses the shipped <c>HasIntervalJsonConversion</c>
/// helper; the optional slot uses the underlying converter directly (EF Core
/// applies a value converter only to the non-null value, so the same converter
/// serves the <c>TInterval?</c> column). Both DbContexts share this so SQL Server
/// and PostgreSQL persist intervals identically.
/// </summary>
internal static class IntervalEntityConfiguration
{
    public static void ConfigureIntervalEntities(this ModelBuilder modelBuilder)
    {
        Configure<ClosedIntervalEntity, ClosedInterval<int>>(modelBuilder);
        Configure<IntervalEntity, Interval<int>>(modelBuilder);
        Configure<IntervalFromEntity, IntervalFrom<int>>(modelBuilder);
        Configure<IntervalUntilEntity, IntervalUntil<int>>(modelBuilder);
    }

    private static void Configure<TEntity, TInterval>(ModelBuilder modelBuilder)
        where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
        where TInterval : struct
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.HasIntervalJsonConversion(e => e.Value);
        entity.Property(e => e.NullableValue).HasConversion(new IntervalJsonValueConverter<TInterval>());
    }
}

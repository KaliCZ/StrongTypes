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

    /// <summary>
    /// Maps the parallel <c>*ColumnsEntity</c> set to the two-scalar-column shape
    /// via <c>HasIntervalColumns</c> (an EF Core complex type). The required
    /// <c>Value</c> uses the helper; the optional slot maps as an optional complex
    /// property directly. Endpoint columns are nullable exactly when the variant's
    /// endpoint is.
    /// </summary>
    public static void ConfigureIntervalColumnEntities(this ModelBuilder modelBuilder)
    {
        ConfigureColumns<ClosedIntervalColumnsEntity, ClosedInterval<int>>(modelBuilder);
        ConfigureColumns<IntervalColumnsEntity, Interval<int>>(modelBuilder);
        ConfigureColumns<IntervalFromColumnsEntity, IntervalFrom<int>>(modelBuilder);
        ConfigureColumns<IntervalUntilColumnsEntity, IntervalUntil<int>>(modelBuilder);
    }

    private static void Configure<TEntity, TInterval>(ModelBuilder modelBuilder)
        where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
        where TInterval : struct
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.HasIntervalJsonConversion(e => e.Value);
        entity.Property(e => e.NullableValue).HasConversion(new IntervalJsonValueConverter<TInterval>());
    }

    private static void ConfigureColumns<TEntity, TInterval>(ModelBuilder modelBuilder)
        where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
        where TInterval : struct
    {
        var entity = modelBuilder.Entity<TEntity>();
        entity.HasIntervalColumns(e => e.Value);
        // The optional slot needs a shadow discriminator: a fully-open Interval<int>
        // flattens to all-null columns, indistinguishable from a null property
        // without it. Harmless (and uniform) for the variants with a required endpoint.
        entity.ComplexProperty(e => e.NullableValue).HasDiscriminator();
    }
}

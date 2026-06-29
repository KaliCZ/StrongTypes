using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StrongTypes.EfCore;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// The <c>UseStrongTypes()</c> convention auto-maps an interval property to a
/// single JSON string column by default — no explicit <c>HasIntervalJsonConversion</c>
/// — and an explicit <c>HasIntervalColumns</c> overrides that to two endpoint
/// columns. Model-shape assertions only; no database, so no containers needed.
/// </summary>
public class IntervalConventionMappingTests
{
    private sealed class Holder
    {
        public Guid Id { get; set; }
        public ClosedInterval<int> Window { get; set; }
    }

    private sealed class DefaultContext : DbContext
    {
        public DbSet<Holder> Holders => Set<Holder>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseInMemoryDatabase(nameof(DefaultContext)).UseStrongTypes();
    }

    private sealed class ColumnsContext : DbContext
    {
        public DbSet<Holder> Holders => Set<Holder>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseInMemoryDatabase(nameof(ColumnsContext)).UseStrongTypes();
        protected override void OnModelCreating(ModelBuilder b) =>
            b.Entity<Holder>().HasIntervalColumns(e => e.Window);
    }

    [Fact]
    public void ConventionAutoMapsIntervalToSingleJsonColumnByDefault()
    {
        using var ctx = new DefaultContext();
        var entity = ctx.Model.FindEntityType(typeof(Holder))!;

        var window = entity.FindProperty(nameof(Holder.Window));
        Assert.NotNull(window);                                       // a scalar property, not complex
        Assert.Equal(typeof(string), window!.GetValueConverter()?.ProviderClrType);   // …round-tripped via JSON string
        Assert.Empty(entity.GetComplexProperties());
    }

    [Fact]
    public void HasIntervalColumnsOptsIntoTwoColumns()
    {
        using var ctx = new ColumnsContext();
        var entity = ctx.Model.FindEntityType(typeof(Holder))!;

        Assert.Null(entity.FindProperty(nameof(Holder.Window)));      // no scalar column
        var complex = entity.GetComplexProperties().Single(p => p.Name == nameof(Holder.Window));
        var endpoints = complex.ComplexType.GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(ClosedInterval<int>.Start), endpoints);
        Assert.Contains(nameof(ClosedInterval<int>.End), endpoints);
    }
}

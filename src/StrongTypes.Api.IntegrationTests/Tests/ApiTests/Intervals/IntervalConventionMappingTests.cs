using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using StrongTypes.EfCore;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// The <c>UseStrongTypes()</c> convention auto-maps an interval property to two
/// endpoint columns by default — no explicit <c>HasIntervalColumns</c> — with a
/// shadow discriminator on the nullable form, and an explicit
/// <c>HasIntervalJsonConversion</c> opts into the single JSON column instead.
/// Model-shape assertions only, against offline relational models: model building
/// never opens a connection, so no connection string is configured and no
/// containers are needed. Real-server round-tripping, ordering, and the full
/// bound-mode matrix are covered by
/// <see cref="IntervalColumnMappingMatrixTestsBase{TEntity, TInterval}"/> against
/// Testcontainers; the InMemory provider is unusable even here because it maps any
/// struct as a scalar, hiding the complex default.
/// </summary>
public class IntervalConventionMappingTests
{
    private sealed class Holder
    {
        public Guid Id { get; set; }
        public FiniteInterval<int> Window { get; set; }
    }

    private sealed class NullableHolder
    {
        public Guid Id { get; set; }
        public Interval<int>? Window { get; set; }
    }

    private sealed class NonPublicBackingHolder
    {
        public Guid Id { get; set; }
        internal Interval<int>? Window { get; set; }
    }

    private sealed class DefaultContext : DbContext
    {
        public DbSet<Holder> Holders => Set<Holder>();
        public DbSet<NullableHolder> NullableHolders => Set<NullableHolder>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseSqlServer().UseStrongTypes();
    }

    // A non-public interval backing property (issue #112 for intervals): EF does not
    // discover it by convention, so it is mapped explicitly as a complex property.
    private sealed class NonPublicBackingContext : DbContext
    {
        public DbSet<NonPublicBackingHolder> Holders => Set<NonPublicBackingHolder>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseSqlServer().UseStrongTypes();
        protected override void OnModelCreating(ModelBuilder b) =>
            b.Entity<NonPublicBackingHolder>().ComplexProperty(nameof(NonPublicBackingHolder.Window));
    }

    private sealed class NpgsqlJsonContext : DbContext
    {
        public DbSet<Holder> Holders => Set<Holder>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseNpgsql().UseStrongTypes();
        protected override void OnModelCreating(ModelBuilder b) =>
            b.Entity<Holder>().HasIntervalJsonConversion(e => e.Window);
    }

    private sealed class SqlServerJsonContext : DbContext
    {
        public DbSet<Holder> Holders => Set<Holder>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseSqlServer().UseStrongTypes();
        protected override void OnModelCreating(ModelBuilder b) =>
            b.Entity<Holder>().HasIntervalJsonConversion(e => e.Window);
    }

    // No UseStrongTypes: the explicit API must stand alone.
    private sealed class ExplicitColumnsContext : DbContext
    {
        public DbSet<Holder> Holders => Set<Holder>();
        public DbSet<NullableHolder> NullableHolders => Set<NullableHolder>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseSqlServer();
        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Holder>().HasIntervalColumns(e => e.Window);
            b.Entity<NullableHolder>().HasIntervalColumns(e => e.Window, endBound: IntervalBoundMode.Stored);
        }
    }

    private sealed class StoredBoundsContext : DbContext
    {
        public DbSet<Holder> Holders => Set<Holder>();
        protected override void OnConfiguring(DbContextOptionsBuilder o) =>
            o.UseSqlServer().UseStrongTypes();
        protected override void OnModelCreating(ModelBuilder b) =>
            b.Entity<Holder>().HasIntervalColumns(e => e.Window, startBound: IntervalBoundMode.Stored, endBound: IntervalBoundMode.Stored);
    }

    [Fact]
    public void HasIntervalColumnsMapsTwoColumnsWithoutTheConvention()
    {
        using var ctx = new ExplicitColumnsContext();

        var complex = ctx.Model.FindEntityType(typeof(Holder))!
            .GetComplexProperties().Single(p => p.Name == nameof(Holder.Window));
        var endpoints = complex.ComplexType.GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(FiniteInterval<int>.Start), endpoints);
        Assert.Contains(nameof(FiniteInterval<int>.End), endpoints);
        Assert.DoesNotContain(nameof(FiniteInterval<int>.StartInclusive), endpoints);
        Assert.DoesNotContain(nameof(FiniteInterval<int>.EndInclusive), endpoints);

        var nullableComplex = ctx.Model.FindEntityType(typeof(NullableHolder))!
            .GetComplexProperties().Single(p => p.Name == nameof(NullableHolder.Window));
        Assert.True(nullableComplex.IsNullable);
        Assert.Single(nullableComplex.ComplexType.GetProperties(), p => p.Name == "Discriminator" && p.IsShadowProperty());

        // endBound: Stored maps only the end flag column.
        var nullableMembers = nullableComplex.ComplexType.GetProperties().Select(p => p.Name).ToHashSet();
        Assert.DoesNotContain(nameof(Interval<int>.StartInclusive), nullableMembers);
        Assert.Contains(nameof(Interval<int>.EndInclusive), nullableMembers);
    }

    [Fact]
    public void ConventionAutoMapsIntervalToTwoColumnsByDefault()
    {
        using var ctx = new DefaultContext();
        var entity = ctx.Model.FindEntityType(typeof(Holder))!;

        Assert.Null(entity.FindProperty(nameof(Holder.Window)));      // no scalar column
        var complex = entity.GetComplexProperties().Single(p => p.Name == nameof(Holder.Window));
        var endpoints = complex.ComplexType.GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(FiniteInterval<int>.Start), endpoints);
        Assert.Contains(nameof(FiniteInterval<int>.End), endpoints);
        Assert.DoesNotContain(nameof(FiniteInterval<int>.StartInclusive), endpoints);
        Assert.DoesNotContain(nameof(FiniteInterval<int>.EndInclusive), endpoints);
    }

    [Fact]
    public void StoredBoundModeAddsFlagColumnsOverTheConventionDefault()
    {
        using var ctx = new StoredBoundsContext();
        var complex = ctx.Model.FindEntityType(typeof(Holder))!
            .GetComplexProperties().Single(p => p.Name == nameof(Holder.Window));

        var members = complex.ComplexType.GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(FiniteInterval<int>.StartInclusive), members);
        Assert.Contains(nameof(FiniteInterval<int>.EndInclusive), members);
        Assert.Equal(typeof(bool), complex.ComplexType.FindProperty(nameof(FiniteInterval<int>.StartInclusive))!.ClrType);
    }

    [Fact]
    public void ConventionAddsDiscriminatorToNullableInterval()
    {
        using var ctx = new DefaultContext();
        var entity = ctx.Model.FindEntityType(typeof(NullableHolder))!;

        var complex = entity.GetComplexProperties().Single(p => p.Name == nameof(NullableHolder.Window));
        Assert.True(complex.IsNullable);
        var discriminator = complex.ComplexType.GetProperties().Single(p => p.Name == "Discriminator");
        Assert.True(discriminator.IsShadowProperty());
        Assert.Equal(typeof(string), discriminator.ClrType);
    }

    [Fact]
    public void ConventionShapesNonPublicIntervalBackingProperty()
    {
        using var ctx = new NonPublicBackingContext();
        var complex = ctx.Model.FindEntityType(typeof(NonPublicBackingHolder))!
            .GetComplexProperties().Single(p => p.Name == nameof(NonPublicBackingHolder.Window));

        Assert.True(complex.IsNullable);
        var members = complex.ComplexType.GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Contains(nameof(Interval<int>.Start), members);
        Assert.Contains(nameof(Interval<int>.End), members);
        Assert.DoesNotContain(nameof(Interval<int>.StartInclusive), members);
        Assert.DoesNotContain(nameof(Interval<int>.EndInclusive), members);
        Assert.Single(complex.ComplexType.GetProperties(), p => p.Name == "Discriminator" && p.IsShadowProperty());
    }

    [Fact]
    public void HasIntervalJsonConversionOptsIntoSingleJsonColumn()
    {
        using var ctx = new SqlServerJsonContext();
        var entity = ctx.Model.FindEntityType(typeof(Holder))!;

        var window = entity.FindProperty(nameof(Holder.Window));
        Assert.NotNull(window);                                       // a scalar property, not complex
        Assert.Equal(typeof(string), window!.GetValueConverter()?.ProviderClrType);   // …round-tripped via JSON string
        Assert.Empty(entity.GetComplexProperties());
    }

    [Fact]
    public void JsonColumnIsJsonbOnPostgreSql()
    {
        using var ctx = new NpgsqlJsonContext();
        var window = ctx.Model.FindEntityType(typeof(Holder))!.FindProperty(nameof(Holder.Window))!;

        Assert.Equal("jsonb", window.GetColumnType());
    }

    [Fact]
    public void JsonColumnStaysStringTypedOnSqlServer()
    {
        using var ctx = new SqlServerJsonContext();
        var window = ctx.Model.FindEntityType(typeof(Holder))!.FindProperty(nameof(Holder.Window))!;

        Assert.Equal("nvarchar(max)", window.GetColumnType());
    }

    [Fact]
    public void StoredBoundMode_MakesTheInclusivityFlagsQueryable()
    {
        using var ctx = new StoredBoundsContext();

        var sql = ctx.Holders.OrderBy(h => h.Window.StartInclusive).ThenBy(h => h.Window.EndInclusive).ToQueryString();
        Assert.Contains(nameof(FiniteInterval<int>.StartInclusive), sql);
        Assert.Contains(nameof(FiniteInterval<int>.EndInclusive), sql);
    }

    [Fact]
    public void DefaultMode_CannotQueryTheInclusivityFlags()
    {
        using var ctx = new DefaultContext();

        Assert.Throws<InvalidOperationException>(() => ctx.Holders.Where(h => h.Window.StartInclusive).ToQueryString());
    }
}

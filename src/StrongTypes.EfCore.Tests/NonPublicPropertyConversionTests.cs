using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit;

namespace StrongTypes.EfCore.Tests;

/// <summary>
/// The convention must wire a value converter onto every mapped strong-type
/// property, including non-public ones (an <c>internal</c>/<c>private</c> DDD
/// backing property) that EF discovers only once they are explicitly configured.
/// Without the converter EF throws "could not be mapped" while validating the
/// model, so building the model offline against the PostgreSQL provider — no
/// server, no container — is enough to catch the regression.
/// </summary>
public sealed class NonPublicPropertyConversionTests
{
    private static readonly IModel Model = BuildModel();

    [Theory]
    [InlineData(nameof(Brand.Name), typeof(NonEmptyStringValueConverter), "text", false)]
    [InlineData(nameof(Brand.AliasesInternal), typeof(NonEmptyStringValueConverter), "text", true)]
    [InlineData(nameof(Brand.RankInternal), typeof(NumericStrongTypeValueConverter<Positive<int>, int>), "integer", false)]
    public void MappedStrongTypeProperty_GetsConverterAndUnderlyingColumn(
        string propertyName, Type expectedConverter, string expectedColumnType, bool expectedNullable)
    {
        var property = Model.FindEntityType(typeof(Brand))!.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.IsType(expectedConverter, property!.GetValueConverter());
        Assert.Equal(expectedColumnType, property.GetColumnType());
        Assert.Equal(expectedNullable, property.IsNullable);
    }

    [Fact]
    public void ExplicitConverter_IsNotReplacedByTheConvention()
    {
        var property = Model.FindEntityType(typeof(Widget))!.FindProperty(nameof(Widget.Label));

        Assert.IsType<CustomLabelConverter>(property!.GetValueConverter());
    }

    private static IModel BuildModel()
    {
        var optionsBuilder = new DbContextOptionsBuilder<StrongTypeDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=none");
        optionsBuilder.UseStrongTypes();
        using var context = new StrongTypeDbContext(optionsBuilder.Options);
        return context.Model;
    }
}

file sealed class Brand
{
    public Guid Id { get; set; }
    public NonEmptyString Name { get; set; } = null!;
    internal NonEmptyString? AliasesInternal { get; set; }
    internal Positive<int> RankInternal { get; set; }
}

file sealed class Widget
{
    public Guid Id { get; set; }
    internal NonEmptyString? Label { get; set; }
}

file sealed class CustomLabelConverter() : ValueConverter<NonEmptyString, string>(
    value => value.Value, value => NonEmptyString.Create(value));

file sealed class StrongTypeDbContext(DbContextOptions<StrongTypeDbContext> options) : DbContext(options)
{
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Widget> Widgets => Set<Widget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.Property(brand => brand.AliasesInternal).HasColumnName("Aliases");
            entity.Property(brand => brand.RankInternal);
        });
        modelBuilder.Entity<Widget>(entity =>
            entity.Property(widget => widget.Label).HasConversion<CustomLabelConverter>());
    }
}

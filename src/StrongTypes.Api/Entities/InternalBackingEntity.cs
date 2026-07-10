using Microsoft.EntityFrameworkCore;

namespace StrongTypes.Api.Entities;

/// <summary>
/// Models issue #112: a nullable strong type held in a non-public EF-mapped
/// backing property. EF does not discover non-public members by convention, so
/// <see cref="BackingNullable"/> is mapped explicitly in <see cref="Configure"/>;
/// the StrongTypes convention must then wire its value converter automatically.
/// </summary>
public sealed class InternalBackingEntity
{
    public Guid Id { get; set; }
    public NonEmptyString Name { get; set; } = null!;

    internal NonEmptyString? BackingNullable { get; set; }

    /// <summary>Public view over the non-public backing property, so tests can read it without exposing it.</summary>
    public NonEmptyString? ReadBacking() => BackingNullable;

    public static InternalBackingEntity Create(NonEmptyString name, NonEmptyString? backing) =>
        new() { Id = Guid.NewGuid(), Name = name, BackingNullable = backing };

    public static void Configure(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<InternalBackingEntity>().Property(entity => entity.BackingNullable);
}

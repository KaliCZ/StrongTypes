using Microsoft.EntityFrameworkCore;

namespace StrongTypes.Api.Entities;

/// <summary>
/// The interval analog of <see cref="InternalBackingEntity"/> (issue #112): a
/// nullable interval held in a non-public EF-mapped backing property. EF does
/// not discover non-public members by convention, so <see cref="BackingWindow"/>
/// is mapped explicitly as a complex property in <see cref="Configure"/>; the
/// StrongTypes convention must then give it the two-endpoint-column shape —
/// including the shadow discriminator that keeps a <c>null</c> property distinct
/// from an unbounded interval — automatically.
/// </summary>
public sealed class InternalBackingIntervalEntity
{
    public Guid Id { get; set; }

    internal Interval<int>? BackingWindow { get; set; }

    /// <summary>Public view over the non-public backing property, so tests can read it without exposing it.</summary>
    public Interval<int>? ReadBacking() => BackingWindow;

    public static InternalBackingIntervalEntity Create(Interval<int>? window) =>
        new() { Id = Guid.NewGuid(), BackingWindow = window };

    public static void Configure(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<InternalBackingIntervalEntity>().ComplexProperty(nameof(BackingWindow));
}

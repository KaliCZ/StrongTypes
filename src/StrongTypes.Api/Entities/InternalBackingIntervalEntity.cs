using Microsoft.EntityFrameworkCore;

namespace StrongTypes.Api.Entities;

/// <summary>
/// The interval analog of <see cref="InternalBackingEntity"/> (issue #112). EF does not discover
/// non-public members, so <see cref="Configure"/> maps <see cref="BackingWindow"/> explicitly;
/// the convention must still shape it as endpoint columns with the shadow discriminator that
/// keeps <c>null</c> distinct from an unbounded interval.
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

#nullable enable
namespace StrongTypes;

/// <summary>How an interval endpoint's inclusivity is represented when the interval is persisted or serialized — used by EF Core's <c>HasIntervalColumns</c> and by <see cref="IntervalJsonConverter{TInterval}"/>.</summary>
public enum IntervalBoundMode
{
    /// <summary>The bound is always inclusive and its flag is never stored; writing a value with an exclusive bound throws.</summary>
    AlwaysInclusive,

    /// <summary>The bound is always exclusive and its flag is never stored; writing a value with an inclusive bound throws, and the exclusive bound is restored on read.</summary>
    AlwaysExclusive,

    /// <summary>The bound's inclusivity travels with each value — an EF Core flag column, or a JSON property written only when it is not the inclusive default.</summary>
    Stored,
}

#nullable enable
namespace StrongTypes;

/// <summary>How an interval endpoint's inclusivity is represented on the JSON wire — the per-bound setting taken by <see cref="IntervalJsonConverter{TInterval}"/>.</summary>
public enum IntervalBoundMode
{
    /// <summary>The bound is always inclusive and its flag is never written; serializing a value with an exclusive bound throws.</summary>
    AlwaysInclusive,

    /// <summary>The bound is always exclusive and its flag is never written; serializing a value with an inclusive bound throws, and the exclusive bound is restored on read.</summary>
    AlwaysExclusive,

    /// <summary>The bound's inclusivity travels with each value — a JSON property written only when it is not the inclusive default.</summary>
    Stored,
}

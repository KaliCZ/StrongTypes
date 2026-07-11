namespace StrongTypes.EfCore;

/// <summary>How an interval endpoint's inclusivity is persisted in the two-column mapping.</summary>
public enum IntervalBoundMode
{
    /// <summary>Every stored bound is inclusive; no flag column. Saving a value with an exclusive bound throws.</summary>
    AlwaysInclusive,

    /// <summary>Every stored bound is exclusive; no flag column. Saving a value with an inclusive bound throws. Requires <c>UseStrongTypes()</c>, which applies the mode when reading.</summary>
    AlwaysExclusive,

    /// <summary>The bound is stored per value in its own column (<c>StartInclusive</c> / <c>EndInclusive</c>).</summary>
    Stored,
}

internal static class IntervalAnnotations
{
    public const string StartBound = "StrongTypes:StartBound";
    public const string EndBound = "StrongTypes:EndBound";
}

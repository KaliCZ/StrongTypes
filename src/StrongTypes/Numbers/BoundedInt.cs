using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>An <see cref="int"/> guaranteed to be within the closed range <c>[TBounds.Min, TBounds.Max]</c>.</summary>
/// <typeparam name="TBounds">A witness type carrying the inclusive lower and upper bounds.</typeparam>
/// <remarks>Construct via <see cref="TryCreate"/> or <c>Create</c>. <c>default(BoundedInt&lt;TBounds&gt;)</c> wraps <c>TBounds.Min</c> and satisfies the invariant.</remarks>
[NumericWrapper(InvariantDescription = "between {TBounds.Min} and {TBounds.Max} (inclusive)")]
[JsonConverter(typeof(NumericStrongTypeJsonConverterFactory))]
public readonly partial struct BoundedInt<TBounds>
    where TBounds : IBounds<int>
{
    // Stored as (Value - TBounds.Min); default(BoundedInt<TBounds>) therefore represents Value == TBounds.Min.
    private readonly int _offset;

    private BoundedInt(int offset)
    {
        _offset = offset;
    }

    [Pure]
    public int Value => _offset + TBounds.Min;

    /// <summary>The inclusive lower bound supplied by <typeparamref name="TBounds"/>.</summary>
    public static int Min => TBounds.Min;

    /// <summary>The inclusive upper bound supplied by <typeparamref name="TBounds"/>.</summary>
    public static int Max => TBounds.Max;

    /// <summary>Wraps <paramref name="value"/>, or returns <c>null</c> when it is outside <c>[TBounds.Min, TBounds.Max]</c>.</summary>
    /// <param name="value">The number to validate.</param>
    [Pure]
    public static BoundedInt<TBounds>? TryCreate(int value)
    {
        return value >= TBounds.Min && value <= TBounds.Max
            ? new BoundedInt<TBounds>(value - TBounds.Min)
            : null;
    }
}

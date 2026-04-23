using System.Diagnostics.Contracts;
using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A numeric value guaranteed to be greater than or equal to <c>T.Zero</c>.
/// </summary>
/// <remarks>
/// Construct via <see cref="TryCreate"/> or <c>Create</c> (generated). Unlike
/// <see cref="Positive{T}"/>, <c>default(NonNegative&lt;T&gt;)</c> wraps
/// <c>T.Zero</c> and therefore <em>does</em> satisfy the invariant, though the
/// factories remain the intended entry point.
/// </remarks>
[NumericWrapper(InvariantDescription = "non-negative", GenerateSum = true)]
[JsonConverter(typeof(NumericStrongTypeJsonConverterFactory))]
public readonly partial struct NonNegative<T>
    where T : INumber<T>
{
    private NonNegative(T value)
    {
        Value = value;
    }

    [Pure]
    public T Value { get; }

    /// <summary>
    /// Returns a <see cref="NonNegative{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> if <paramref name="value"/> is less than zero.
    /// </summary>
    [Pure]
    public static NonNegative<T>? TryCreate(T value)
    {
        return value >= T.Zero ? new NonNegative<T>(value) : null;
    }
}

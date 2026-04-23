using System.Diagnostics.Contracts;
using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A numeric value guaranteed to be less than or equal to <c>T.Zero</c>.
/// </summary>
/// <remarks>
/// Construct via <see cref="TryCreate"/> or <c>Create</c> (generated). Unlike
/// <see cref="Negative{T}"/>, <c>default(NonPositive&lt;T&gt;)</c> wraps
/// <c>T.Zero</c> and therefore <em>does</em> satisfy the invariant, though the
/// factories remain the intended entry point.
/// </remarks>
[NumericWrapper(InvariantDescription = "non-positive", GenerateSum = true)]
[JsonConverter(typeof(NumericStrongTypeJsonConverterFactory))]
public readonly partial struct NonPositive<T>
    where T : INumber<T>
{
    private NonPositive(T value)
    {
        Value = value;
    }

    [Pure]
    public T Value { get; }

    /// <summary>
    /// Returns a <see cref="NonPositive{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> if <paramref name="value"/> is greater than zero.
    /// </summary>
    [Pure]
    public static NonPositive<T>? TryCreate(T value)
    {
        return value <= T.Zero ? new NonPositive<T>(value) : null;
    }
}

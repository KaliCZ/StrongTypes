#nullable enable

using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A numeric value guaranteed to be strictly greater than <c>T.Zero</c>.
/// </summary>
/// <remarks>
/// Construct via <see cref="TryCreate"/> or <c>Create</c> (generated). Internally
/// the value is stored as an offset from <c>T.One</c> so that
/// <c>default(Positive&lt;T&gt;)</c> represents <c>T.One</c> — i.e. the
/// zero-initialized struct still satisfies the positivity invariant.
/// </remarks>
[NumericWrapper(InvariantDescription = "positive", GenerateSum = true)]
[JsonConverter(typeof(NumericStrongTypeJsonConverterFactory))]
public readonly partial struct Positive<T>
    where T : INumber<T>
{
    // Stored as (Value - T.One); default(Positive<T>) therefore represents Value == T.One.
    private readonly T _offset;

    private Positive(T offset)
    {
        _offset = offset;
    }

    public T Value => _offset + T.One;

    /// <summary>
    /// Returns a <see cref="Positive{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> if <paramref name="value"/> is not strictly greater than zero.
    /// </summary>
    public static Positive<T>? TryCreate(T value)
    {
        return value > T.Zero ? new Positive<T>(value - T.One) : null;
    }
}

using System.Diagnostics.Contracts;
using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A numeric value guaranteed to be strictly less than <c>T.Zero</c>.
/// </summary>
/// <remarks>
/// Construct via <see cref="TryCreate"/> or <c>Create</c> (generated). Internally
/// the value is stored as an offset from <c>-T.One</c> so that
/// <c>default(Negative&lt;T&gt;)</c> represents <c>-T.One</c> — i.e. the
/// zero-initialized struct still satisfies the negativity invariant.
/// </remarks>
[NumericWrapper(InvariantDescription = "negative", GenerateSum = true)]
[JsonConverter(typeof(NumericStrongTypeJsonConverterFactory))]
public readonly partial struct Negative<T>
    where T : INumber<T>
{
    // Stored as (Value - (-T.One)) == (Value + T.One); default represents Value == -T.One.
    private readonly T _offset;

    private Negative(T offset)
    {
        _offset = offset;
    }

    [Pure]
    public T Value => _offset - T.One;

    /// <summary>
    /// Returns a <see cref="Negative{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> if <paramref name="value"/> is not strictly less than zero.
    /// </summary>
    [Pure]
    public static Negative<T>? TryCreate(T value)
    {
        return value < T.Zero ? new Negative<T>(value + T.One) : null;
    }
}

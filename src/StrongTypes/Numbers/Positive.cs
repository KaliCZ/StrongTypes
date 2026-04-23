#nullable enable

using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>A numeric value guaranteed to be strictly greater than <c>T.Zero</c>.</summary>
/// <typeparam name="T">The underlying numeric type.</typeparam>
/// <remarks>Construct via <see cref="TryCreate"/> or <c>Create</c>. <c>default(Positive&lt;T&gt;)</c> wraps <c>T.One</c> and satisfies the invariant.</remarks>
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

    /// <summary>Wraps <paramref name="value"/>, or returns <c>null</c> when it is not strictly greater than zero.</summary>
    /// <param name="value">The number to validate.</param>
    public static Positive<T>? TryCreate(T value)
    {
        return value > T.Zero ? new Positive<T>(value - T.One) : null;
    }
}

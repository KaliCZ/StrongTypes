#nullable enable

using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>A numeric value guaranteed to be strictly less than <c>T.Zero</c>.</summary>
/// <typeparam name="T">The underlying numeric type.</typeparam>
/// <remarks>Construct via <see cref="TryCreate"/> or <c>Create</c>. <c>default(Negative&lt;T&gt;)</c> wraps <c>-T.One</c> and satisfies the invariant.</remarks>
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

    public T Value => _offset - T.One;

    /// <summary>Wraps <paramref name="value"/>, or returns <c>null</c> when it is not strictly less than zero.</summary>
    /// <param name="value">The number to validate.</param>
    public static Negative<T>? TryCreate(T value)
    {
        return value < T.Zero ? new Negative<T>(value + T.One) : null;
    }
}

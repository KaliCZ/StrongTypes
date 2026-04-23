using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static class DigitExtensions
{
    /// <summary>
    /// Returns a <see cref="Digit"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> if <paramref name="value"/> is not a decimal digit character.
    /// </summary>
    [Pure]
    public static Digit? AsDigit(this char value) => Digit.TryCreate(value);

    /// <summary>
    /// Returns the decimal digits in <paramref name="value"/>, in order.
    /// A <c>null</c> input yields an empty sequence.
    /// </summary>
    [Pure]
    public static IEnumerable<Digit> FilterDigits(this string? value)
    {
        if (value is null)
        {
            return Enumerable.Empty<Digit>();
        }

        return value.Select(Digit.TryCreate).ExceptNulls();
    }
}

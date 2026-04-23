#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

public static class DigitExtensions
{
    /// <summary>Returns a <see cref="Digit"/> wrapping <paramref name="value"/>, or <c>null</c> when it is not a decimal digit character.</summary>
    /// <param name="value">The character to parse.</param>
    public static Digit? AsDigit(this char value) => Digit.TryCreate(value);

    /// <summary>Returns the decimal digits in <paramref name="value"/>, in order. A <c>null</c> input yields an empty sequence.</summary>
    /// <param name="value">The string to scan.</param>
    public static IEnumerable<Digit> FilterDigits(this string? value)
    {
        if (value is null)
        {
            return Enumerable.Empty<Digit>();
        }

        return value.Select(Digit.TryCreate).ExceptNulls();
    }
}

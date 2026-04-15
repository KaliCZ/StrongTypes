#nullable enable

namespace StrongTypes;

public static class StringExtensions
{
    /// <summary>
    /// Returns a <see cref="NonEmptyString"/> wrapping <paramref name="s"/>, or
    /// <c>null</c> if <paramref name="s"/> is null, empty, or whitespace.
    /// </summary>
    public static NonEmptyString? AsNonEmpty(this string? s) => NonEmptyString.TryCreate(s);
}

using Xunit;

namespace StrongTypes.Tests;

/// <summary>
/// Asserts over the legacy <see cref="Option{T}"/> type, still needed by _Old
/// tests that cover Option-returning APIs (Aggregate, AsNonEmpty, …) which
/// haven't been migrated off Option yet.
/// </summary>
public static class OptionAssert
{
    public static void IsEmpty<T>(Option<T> option) =>
        Assert.True(option.IsEmpty, "Option was expected to be empty, but had a value.");

    public static void NonEmpty<T>(Option<T> option) =>
        Assert.True(option.NonEmpty, "Option was expected to have a value, but was empty.");

    public static void NonEmptyWithValue<T>(T expected, Option<T> option)
    {
        Assert.True(option.NonEmpty, "Option was expected to have a value, but was empty.");
        Assert.Equal(expected, option.Get());
    }
}

#nullable enable

namespace StrongTypes;

public static class NumberExtensions
{
    /// <summary>
    /// Returns <paramref name="a"/> divided by <paramref name="b"/>, or <c>null</c>
    /// when <paramref name="b"/> is zero.
    /// </summary>
    public static decimal? Divide(this int a, decimal b)
        => b == 0 ? null : a / b;

    /// <summary>
    /// Returns <paramref name="a"/> divided by <paramref name="b"/>, or <c>null</c>
    /// when <paramref name="b"/> is zero.
    /// </summary>
    public static decimal? Divide(this decimal a, decimal b)
        => b == 0 ? null : a / b;

    /// <summary>
    /// Returns <paramref name="a"/> divided by <paramref name="b"/>, or
    /// <paramref name="otherwise"/> when <paramref name="b"/> is zero.
    /// </summary>
    public static decimal SafeDivide(this int a, decimal b, decimal otherwise = 0)
        => a.Divide(b) ?? otherwise;

    /// <summary>
    /// Returns <paramref name="a"/> divided by <paramref name="b"/>, or
    /// <paramref name="otherwise"/> when <paramref name="b"/> is zero.
    /// </summary>
    public static decimal SafeDivide(this decimal a, decimal b, decimal otherwise = 0)
        => a.Divide(b) ?? otherwise;
}

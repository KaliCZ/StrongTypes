namespace StrongTypes;

public static class NumberExtensions
{
    #region NonPositive numeric types

    public static Option<NonPositiveShort> AsNonPositive(this short value)
    {
        return NonPositiveShort.Create(value);
    }

    public static NonPositiveShort AsUnsafeNonPositive(this short value)
    {
        return NonPositiveShort.CreateUnsafe(value);
    }

    public static Option<NonPositiveInt> AsNonPositive(this int value)
    {
        return NonPositiveInt.Create(value);
    }

    public static NonPositiveInt AsUnsafeNonPositive(this int value)
    {
        return NonPositiveInt.CreateUnsafe(value);
    }

    public static Option<NonPositiveLong> AsNonPositive(this long value)
    {
        return NonPositiveLong.Create(value);
    }

    public static NonPositiveLong AsUnsafeNonPositive(this long value)
    {
        return NonPositiveLong.CreateUnsafe(value);
    }

    public static Option<NonPositiveDecimal> AsNonPositive(this decimal value)
    {
        return NonPositiveDecimal.Create(value);
    }

    public static NonPositiveDecimal AsUnsafeNonPositive(this decimal value)
    {
        return NonPositiveDecimal.CreateUnsafe(value);
    }

    #endregion

    public static decimal SafeDivide(this int a, decimal b, decimal otherwise = 0)
    {
        return b == 0
            ? otherwise
            : a / b;
    }

    public static decimal SafeDivide(this decimal a, decimal b, decimal otherwise = 0)
    {
        return b == 0
            ? otherwise
            : a / b;
    }

    public static Option<decimal> Divide(this int a, decimal b)
    {
        return b == 0
            ? Option.Empty<decimal>()
            : Option.Valued(a / b);
    }

    public static Option<decimal> Divide(this decimal a, decimal b)
    {
        return b == 0
            ? Option.Empty<decimal>()
            : Option.Valued(a / b);
    }
}
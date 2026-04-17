namespace StrongTypes;

public static class NumberExtensions
{
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
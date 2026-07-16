using System;
using System.Diagnostics.Contracts;

namespace StrongTypes;

public static class BooleanExtensions
{
    /// <summary>Logical implication (<c>!condition || consequence</c>).</summary>
    [Pure]
    public static bool Implies(this bool condition, bool consequence) => !condition || consequence;

    /// <summary>Logical implication with a deferred consequent.</summary>
    /// <param name="condition">The antecedent.</param>
    /// <param name="consequence">Invoked only when <paramref name="condition"/> is <c>true</c>.</param>
    [Pure]
    public static bool Implies(this bool condition, Func<bool> consequence) => !condition || consequence();
}

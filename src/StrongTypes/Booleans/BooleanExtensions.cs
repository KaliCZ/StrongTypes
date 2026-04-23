#nullable enable

using System;

namespace StrongTypes;

public static class BooleanExtensions
{
    /// <summary>Logical implication (<c>!condition || consequence</c>).</summary>
    /// <param name="condition">The antecedent.</param>
    /// <param name="consequence">The consequent. Evaluated eagerly.</param>
    /// <returns><c>true</c> when <paramref name="condition"/> is <c>false</c>, or when both operands are <c>true</c>.</returns>
    public static bool Implies(this bool condition, bool consequence) => !condition || consequence;

    /// <summary>Logical implication with a deferred consequent.</summary>
    /// <param name="condition">The antecedent.</param>
    /// <param name="consequence">Invoked only when <paramref name="condition"/> is <c>true</c>.</param>
    /// <returns><c>true</c> when <paramref name="condition"/> is <c>false</c>, or when <paramref name="consequence"/> returns <c>true</c>.</returns>
    public static bool Implies(this bool condition, Func<bool> consequence) => !condition || consequence();
}

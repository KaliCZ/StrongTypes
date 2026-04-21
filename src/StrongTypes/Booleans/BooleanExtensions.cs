#nullable enable

using System;

namespace StrongTypes;

public static class BooleanExtensions
{
    /// <summary>
    /// Logical implication: returns <c>true</c> when <paramref name="condition"/>
    /// is <c>false</c>, or when both operands are <c>true</c>. Both operands are
    /// evaluated eagerly; use the <see cref="Func{TResult}"/> overload to defer
    /// evaluation of the consequence.
    /// </summary>
    public static bool Implies(this bool condition, bool consequence) => !condition || consequence;

    /// <summary>
    /// Logical implication: returns <c>true</c> when <paramref name="condition"/>
    /// is <c>false</c>, or when both operands are <c>true</c>. Short-circuits —
    /// <paramref name="consequence"/> is only invoked when
    /// <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    public static bool Implies(this bool condition, Func<bool> consequence) => !condition || consequence();
}

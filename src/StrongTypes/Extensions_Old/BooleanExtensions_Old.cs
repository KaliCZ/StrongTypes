using System;
using System.Threading.Tasks;

namespace StrongTypes;

public static class BooleanExtensions
{
    /// <summary>
    /// Returns the result of implication. So either the boolean is false or both have to be true.
    /// </summary>
    public static bool Implies(this bool a, Func<Unit, bool> b)
    {
        return !a || b(Unit.Value);
    }

    /// <summary>
    /// Returns a result of the function depending on the value of the boolean.
    /// </summary>
    public static R Match<R>(this bool b, Func<Unit, R> ifTrue, Func<Unit, R> ifFalse)
    {
        if (b)
        {
            return ifTrue(Unit.Value);
        }
        else
        {
            return ifFalse(Unit.Value);
        }
    }

    /// <summary>
    /// If value is true, return result of map function. Otherwise return an empty option.
    /// </summary>
    public static void Match(this bool b, Action<Unit> ifTrue, Action<Unit> ifFalse)
    {
        if (b)
        {
            ifTrue(Unit.Value);
        }
        else
        {
            ifFalse(Unit.Value);
        }
    }

    /// <summary>
    /// If value is true, return result of map function. Otherwise return an empty option.
    /// </summary>
    public static async Task MatchAsync(this bool value, Func<Unit, Task> ifTrue, Func<Unit, Task> ifFalse)
    {
        if (value)
        {
            await ifTrue(Unit.Value);
        }
        else
        {
            await ifFalse(Unit.Value);
        }
    }

    /// <summary>
    /// If value is true, return result of map function. Otherwise return an empty option.
    /// </summary>
    public static async Task<T> MatchAsync<T>(this bool value, Func<Unit, Task<T>> ifTrue, Func<Unit, Task<T>> ifFalse)
    {
        if (value)
        {
            return await ifTrue(Unit.Value);
        }
        else
        {
            return await ifFalse(Unit.Value);
        }
    }

    /// <summary>
    /// If value is true, return success. Otherwise return an error.
    /// </summary>
    public static Try<bool, E> ToTry<E>(this bool value, Func<Unit, E> e) where E : notnull
    {
        if (value)
        {
            return Try.Success<bool, E>(value);
        }
        else
        {
            return Try.Error<bool, E>(e(Unit.Value));
        }
    }

    /// <summary>
    /// If value is true, return success. Otherwise return an error.
    /// </summary>
    public static Try<T, E> ToTry<T, E>(this bool value, Func<Unit, T> ifTrue, Func<Unit, E> ifFalse)
        where T : notnull
        where E : notnull
    {
        if (value)
        {
            return Try.Success<T, E>(ifTrue(Unit.Value));
        }
        else
        {
            return Try.Error<T, E>(ifFalse(Unit.Value));
        }
    }
}
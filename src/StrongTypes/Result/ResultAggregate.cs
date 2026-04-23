using System;
using System.Collections.Generic;

namespace StrongTypes;

public static partial class Result
{
    /// <summary>
    /// Combines results into a tuple of values on all-success, or an array of
    /// every collected error (not just the first) otherwise.
    /// </summary>
    public static Result<(T1, T2), TError[]> Aggregate<T1, T2, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2)
        where T1 : notnull where T2 : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0);
        if (count == 0) return (r1.InternalValue, r2.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        return errors;
    }

    /// <summary>
    /// Combiner form: on all-success invokes <paramref name="combine"/>; otherwise
    /// returns all collected errors.
    /// </summary>
    public static Result<R, TError[]> Aggregate<T1, T2, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2,
        Func<T1, T2, R> combine)
        where T1 : notnull where T2 : notnull where R : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0);
        if (count == 0) return combine(r1.InternalValue, r2.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        return errors;
    }

    public static Result<(T1, T2, T3), TError[]> Aggregate<T1, T2, T3, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3)
        where T1 : notnull where T2 : notnull where T3 : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0);
        if (count == 0) return (r1.InternalValue, r2.InternalValue, r3.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        return errors;
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3,
        Func<T1, T2, T3, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where R : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0);
        if (count == 0) return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        return errors;
    }

    public static Result<(T1, T2, T3, T4), TError[]> Aggregate<T1, T2, T3, T4, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0) + (r4.IsError ? 1 : 0);
        if (count == 0) return (r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        if (r4.IsError) errors[i++] = r4.InternalError;
        return errors;
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, T4, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4,
        Func<T1, T2, T3, T4, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where R : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0) + (r4.IsError ? 1 : 0);
        if (count == 0) return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        if (r4.IsError) errors[i++] = r4.InternalError;
        return errors;
    }

    public static Result<(T1, T2, T3, T4, T5), TError[]> Aggregate<T1, T2, T3, T4, T5, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0) + (r4.IsError ? 1 : 0) + (r5.IsError ? 1 : 0);
        if (count == 0) return (r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        if (r4.IsError) errors[i++] = r4.InternalError;
        if (r5.IsError) errors[i++] = r5.InternalError;
        return errors;
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, T4, T5, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5,
        Func<T1, T2, T3, T4, T5, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where R : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0) + (r4.IsError ? 1 : 0) + (r5.IsError ? 1 : 0);
        if (count == 0) return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        if (r4.IsError) errors[i++] = r4.InternalError;
        if (r5.IsError) errors[i++] = r5.InternalError;
        return errors;
    }

    public static Result<(T1, T2, T3, T4, T5, T6), TError[]> Aggregate<T1, T2, T3, T4, T5, T6, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0) + (r4.IsError ? 1 : 0) + (r5.IsError ? 1 : 0) + (r6.IsError ? 1 : 0);
        if (count == 0) return (r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        if (r4.IsError) errors[i++] = r4.InternalError;
        if (r5.IsError) errors[i++] = r5.InternalError;
        if (r6.IsError) errors[i++] = r6.InternalError;
        return errors;
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, T4, T5, T6, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6,
        Func<T1, T2, T3, T4, T5, T6, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where R : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0) + (r4.IsError ? 1 : 0) + (r5.IsError ? 1 : 0) + (r6.IsError ? 1 : 0);
        if (count == 0) return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        if (r4.IsError) errors[i++] = r4.InternalError;
        if (r5.IsError) errors[i++] = r5.InternalError;
        if (r6.IsError) errors[i++] = r6.InternalError;
        return errors;
    }

    public static Result<(T1, T2, T3, T4, T5, T6, T7), TError[]> Aggregate<T1, T2, T3, T4, T5, T6, T7, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6, Result<T7, TError> r7)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where T7 : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0) + (r4.IsError ? 1 : 0) + (r5.IsError ? 1 : 0) + (r6.IsError ? 1 : 0) + (r7.IsError ? 1 : 0);
        if (count == 0) return (r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue, r7.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        if (r4.IsError) errors[i++] = r4.InternalError;
        if (r5.IsError) errors[i++] = r5.InternalError;
        if (r6.IsError) errors[i++] = r6.InternalError;
        if (r7.IsError) errors[i++] = r7.InternalError;
        return errors;
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, T4, T5, T6, T7, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6, Result<T7, TError> r7,
        Func<T1, T2, T3, T4, T5, T6, T7, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where T7 : notnull where R : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0) + (r4.IsError ? 1 : 0) + (r5.IsError ? 1 : 0) + (r6.IsError ? 1 : 0) + (r7.IsError ? 1 : 0);
        if (count == 0) return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue, r7.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        if (r4.IsError) errors[i++] = r4.InternalError;
        if (r5.IsError) errors[i++] = r5.InternalError;
        if (r6.IsError) errors[i++] = r6.InternalError;
        if (r7.IsError) errors[i++] = r7.InternalError;
        return errors;
    }

    public static Result<(T1, T2, T3, T4, T5, T6, T7, T8), TError[]> Aggregate<T1, T2, T3, T4, T5, T6, T7, T8, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6, Result<T7, TError> r7, Result<T8, TError> r8)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where T7 : notnull where T8 : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0) + (r4.IsError ? 1 : 0) + (r5.IsError ? 1 : 0) + (r6.IsError ? 1 : 0) + (r7.IsError ? 1 : 0) + (r8.IsError ? 1 : 0);
        if (count == 0) return (r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue, r7.InternalValue, r8.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        if (r4.IsError) errors[i++] = r4.InternalError;
        if (r5.IsError) errors[i++] = r5.InternalError;
        if (r6.IsError) errors[i++] = r6.InternalError;
        if (r7.IsError) errors[i++] = r7.InternalError;
        if (r8.IsError) errors[i++] = r8.InternalError;
        return errors;
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, T4, T5, T6, T7, T8, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6, Result<T7, TError> r7, Result<T8, TError> r8,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where T7 : notnull where T8 : notnull where R : notnull where TError : notnull
    {
        var count = (r1.IsError ? 1 : 0) + (r2.IsError ? 1 : 0) + (r3.IsError ? 1 : 0) + (r4.IsError ? 1 : 0) + (r5.IsError ? 1 : 0) + (r6.IsError ? 1 : 0) + (r7.IsError ? 1 : 0) + (r8.IsError ? 1 : 0);
        if (count == 0) return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue, r7.InternalValue, r8.InternalValue);
        var errors = new TError[count];
        var i = 0;
        if (r1.IsError) errors[i++] = r1.InternalError;
        if (r2.IsError) errors[i++] = r2.InternalError;
        if (r3.IsError) errors[i++] = r3.InternalError;
        if (r4.IsError) errors[i++] = r4.InternalError;
        if (r5.IsError) errors[i++] = r5.InternalError;
        if (r6.IsError) errors[i++] = r6.InternalError;
        if (r7.IsError) errors[i++] = r7.InternalError;
        if (r8.IsError) errors[i++] = r8.InternalError;
        return errors;
    }

    /// <summary>
    /// Aggregates any number of results, collecting every error. The sequence is
    /// fully drained whether or not an error is seen.
    /// </summary>
    public static Result<T[], TError[]> Aggregate<T, TError>(
        IEnumerable<Result<T, TError>> results)
        where T : notnull where TError : notnull
    {
        var capacity = results is ICollection<Result<T, TError>> c ? c.Count : 0;
        var successes = new List<T>(capacity);
        List<TError>? errors = null;
        foreach (var r in results)
        {
            if (r.IsSuccess) successes.Add(r.InternalValue);
            else (errors ??= new List<TError>()).Add(r.InternalError);
        }
        if (errors is not null) return errors.ToArray();
        return successes.ToArray();
    }

    /// <summary>
    /// Combiner form that also folds the collected errors: on all-success
    /// invokes <paramref name="combine"/>; otherwise passes the collected
    /// <typeparamref name="TError"/> array through <paramref name="errorMap"/>
    /// — typically used to join multiple validation messages into a single
    /// <typeparamref name="UError"/>.
    /// </summary>
    public static Result<R, UError> Aggregate<T1, T2, R, TError, UError>(
        Result<T1, TError> r1, Result<T2, TError> r2,
        Func<T1, T2, R> combine,
        Func<TError[], UError> errorMap)
        where T1 : notnull where T2 : notnull where R : notnull
        where TError : notnull where UError : notnull
        => Aggregate(r1, r2, combine).MapError(errorMap);

    public static Result<R, UError> Aggregate<T1, T2, T3, R, TError, UError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3,
        Func<T1, T2, T3, R> combine,
        Func<TError[], UError> errorMap)
        where T1 : notnull where T2 : notnull where T3 : notnull where R : notnull
        where TError : notnull where UError : notnull
        => Aggregate(r1, r2, r3, combine).MapError(errorMap);

    public static Result<R, UError> Aggregate<T1, T2, T3, T4, R, TError, UError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4,
        Func<T1, T2, T3, T4, R> combine,
        Func<TError[], UError> errorMap)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where R : notnull
        where TError : notnull where UError : notnull
        => Aggregate(r1, r2, r3, r4, combine).MapError(errorMap);

    public static Result<R, UError> Aggregate<T1, T2, T3, T4, T5, R, TError, UError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5,
        Func<T1, T2, T3, T4, T5, R> combine,
        Func<TError[], UError> errorMap)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where R : notnull
        where TError : notnull where UError : notnull
        => Aggregate(r1, r2, r3, r4, r5, combine).MapError(errorMap);

    public static Result<R, UError> Aggregate<T1, T2, T3, T4, T5, T6, R, TError, UError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6,
        Func<T1, T2, T3, T4, T5, T6, R> combine,
        Func<TError[], UError> errorMap)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where R : notnull
        where TError : notnull where UError : notnull
        => Aggregate(r1, r2, r3, r4, r5, r6, combine).MapError(errorMap);

    public static Result<R, UError> Aggregate<T1, T2, T3, T4, T5, T6, T7, R, TError, UError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6, Result<T7, TError> r7,
        Func<T1, T2, T3, T4, T5, T6, T7, R> combine,
        Func<TError[], UError> errorMap)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where T7 : notnull where R : notnull
        where TError : notnull where UError : notnull
        => Aggregate(r1, r2, r3, r4, r5, r6, r7, combine).MapError(errorMap);

    public static Result<R, UError> Aggregate<T1, T2, T3, T4, T5, T6, T7, T8, R, TError, UError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6, Result<T7, TError> r7, Result<T8, TError> r8,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, R> combine,
        Func<TError[], UError> errorMap)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where T7 : notnull where T8 : notnull where R : notnull
        where TError : notnull where UError : notnull
        => Aggregate(r1, r2, r3, r4, r5, r6, r7, r8, combine).MapError(errorMap);

    public static Result<T[], UError> Aggregate<T, TError, UError>(
        IEnumerable<Result<T, TError>> results,
        Func<TError[], UError> errorMap)
        where T : notnull where TError : notnull where UError : notnull
        => Aggregate(results).MapError(errorMap);
}

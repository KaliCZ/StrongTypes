#nullable enable

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
        if (r1.IsSuccess && r2.IsSuccess) return (r1.InternalValue, r2.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        return errors.ToArray();
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
        if (r1.IsSuccess && r2.IsSuccess) return combine(r1.InternalValue, r2.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        return errors.ToArray();
    }

    public static Result<(T1, T2, T3), TError[]> Aggregate<T1, T2, T3, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3)
        where T1 : notnull where T2 : notnull where T3 : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess)
            return (r1.InternalValue, r2.InternalValue, r3.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        return errors.ToArray();
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3,
        Func<T1, T2, T3, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where R : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess)
            return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        return errors.ToArray();
    }

    public static Result<(T1, T2, T3, T4), TError[]> Aggregate<T1, T2, T3, T4, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess)
            return (r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        if (r4.IsError) errors.Add(r4.InternalError);
        return errors.ToArray();
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, T4, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4,
        Func<T1, T2, T3, T4, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where R : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess)
            return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        if (r4.IsError) errors.Add(r4.InternalError);
        return errors.ToArray();
    }

    public static Result<(T1, T2, T3, T4, T5), TError[]> Aggregate<T1, T2, T3, T4, T5, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess && r5.IsSuccess)
            return (r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        if (r4.IsError) errors.Add(r4.InternalError);
        if (r5.IsError) errors.Add(r5.InternalError);
        return errors.ToArray();
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, T4, T5, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5,
        Func<T1, T2, T3, T4, T5, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where R : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess && r5.IsSuccess)
            return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        if (r4.IsError) errors.Add(r4.InternalError);
        if (r5.IsError) errors.Add(r5.InternalError);
        return errors.ToArray();
    }

    public static Result<(T1, T2, T3, T4, T5, T6), TError[]> Aggregate<T1, T2, T3, T4, T5, T6, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess && r5.IsSuccess && r6.IsSuccess)
            return (r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        if (r4.IsError) errors.Add(r4.InternalError);
        if (r5.IsError) errors.Add(r5.InternalError);
        if (r6.IsError) errors.Add(r6.InternalError);
        return errors.ToArray();
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, T4, T5, T6, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6,
        Func<T1, T2, T3, T4, T5, T6, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where R : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess && r5.IsSuccess && r6.IsSuccess)
            return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        if (r4.IsError) errors.Add(r4.InternalError);
        if (r5.IsError) errors.Add(r5.InternalError);
        if (r6.IsError) errors.Add(r6.InternalError);
        return errors.ToArray();
    }

    public static Result<(T1, T2, T3, T4, T5, T6, T7), TError[]> Aggregate<T1, T2, T3, T4, T5, T6, T7, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6, Result<T7, TError> r7)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where T7 : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess && r5.IsSuccess && r6.IsSuccess && r7.IsSuccess)
            return (r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue, r7.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        if (r4.IsError) errors.Add(r4.InternalError);
        if (r5.IsError) errors.Add(r5.InternalError);
        if (r6.IsError) errors.Add(r6.InternalError);
        if (r7.IsError) errors.Add(r7.InternalError);
        return errors.ToArray();
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, T4, T5, T6, T7, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6, Result<T7, TError> r7,
        Func<T1, T2, T3, T4, T5, T6, T7, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where T7 : notnull where R : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess && r5.IsSuccess && r6.IsSuccess && r7.IsSuccess)
            return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue, r7.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        if (r4.IsError) errors.Add(r4.InternalError);
        if (r5.IsError) errors.Add(r5.InternalError);
        if (r6.IsError) errors.Add(r6.InternalError);
        if (r7.IsError) errors.Add(r7.InternalError);
        return errors.ToArray();
    }

    public static Result<(T1, T2, T3, T4, T5, T6, T7, T8), TError[]> Aggregate<T1, T2, T3, T4, T5, T6, T7, T8, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6, Result<T7, TError> r7, Result<T8, TError> r8)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where T7 : notnull where T8 : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess && r5.IsSuccess && r6.IsSuccess && r7.IsSuccess && r8.IsSuccess)
            return (r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue, r7.InternalValue, r8.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        if (r4.IsError) errors.Add(r4.InternalError);
        if (r5.IsError) errors.Add(r5.InternalError);
        if (r6.IsError) errors.Add(r6.InternalError);
        if (r7.IsError) errors.Add(r7.InternalError);
        if (r8.IsError) errors.Add(r8.InternalError);
        return errors.ToArray();
    }

    public static Result<R, TError[]> Aggregate<T1, T2, T3, T4, T5, T6, T7, T8, R, TError>(
        Result<T1, TError> r1, Result<T2, TError> r2, Result<T3, TError> r3, Result<T4, TError> r4, Result<T5, TError> r5, Result<T6, TError> r6, Result<T7, TError> r7, Result<T8, TError> r8,
        Func<T1, T2, T3, T4, T5, T6, T7, T8, R> combine)
        where T1 : notnull where T2 : notnull where T3 : notnull where T4 : notnull where T5 : notnull where T6 : notnull where T7 : notnull where T8 : notnull where R : notnull where TError : notnull
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess && r5.IsSuccess && r6.IsSuccess && r7.IsSuccess && r8.IsSuccess)
            return combine(r1.InternalValue, r2.InternalValue, r3.InternalValue, r4.InternalValue, r5.InternalValue, r6.InternalValue, r7.InternalValue, r8.InternalValue);
        var errors = new List<TError>();
        if (r1.IsError) errors.Add(r1.InternalError);
        if (r2.IsError) errors.Add(r2.InternalError);
        if (r3.IsError) errors.Add(r3.InternalError);
        if (r4.IsError) errors.Add(r4.InternalError);
        if (r5.IsError) errors.Add(r5.InternalError);
        if (r6.IsError) errors.Add(r6.InternalError);
        if (r7.IsError) errors.Add(r7.InternalError);
        if (r8.IsError) errors.Add(r8.InternalError);
        return errors.ToArray();
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
}

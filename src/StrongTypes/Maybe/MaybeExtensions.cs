#nullable enable

using System;
using System.Threading.Tasks;

namespace StrongTypes;

// C# 14 extension members: `Maybe<T>.Value` surfaces the underlying value as a
// nullable (`Nullable<T>` for structs, `T?` for references), enabling the
// `if (maybe.Value is {} v)` pattern to unwrap in one expression. The two branches
// live in separate static classes because the generated `get_Value` signatures
// collide when placed in the same containing type.
public static class MaybeStructValueExtensions
{
    extension<T>(Maybe<T> m) where T : struct
    {
        public T? Value => m.HasValue ? m.InternalValue : null;
    }
}

public static class MaybeClassValueExtensions
{
    extension<T>(Maybe<T> m) where T : class
    {
        public T? Value => m.HasValue ? m.InternalValue : null;
    }
}

public static class MaybeExtensions
{
    public static Maybe<T> ToMaybe<T>(this T? value)
        where T : struct
        => value.HasValue ? Maybe<T>.Some(value.Value) : default;

    public static Maybe<T> ToMaybe<T>(this T? value)
        where T : class
        => value is not null ? Maybe<T>.Some(value) : default;

    public static async Task<Maybe<B>> MapAsync<A, B>(this Maybe<A> m, Func<A, Task<B>> f)
        where A : notnull
        where B : notnull
        => m.HasValue ? Maybe<B>.Some(await f(m.InternalValue)) : default;

    public static async Task MatchAsync<A>(
        this Maybe<A> m,
        Func<A, Task> ifSome,
        Func<Task>? ifNone = null)
        where A : notnull
    {
        if (m.HasValue) await ifSome(m.InternalValue);
        else if (ifNone is not null) await ifNone();
    }

    public static async Task<R> MatchAsync<A, R>(
        this Maybe<A> m,
        Func<A, Task<R>> ifSome,
        Func<Task<R>> ifNone)
        where A : notnull
        => m.HasValue ? await ifSome(m.InternalValue) : await ifNone();

    public static async Task<Maybe<B>> FlatMapAsync<A, B>(this Maybe<A> m, Func<A, Task<Maybe<B>>> f)
        where A : notnull
        where B : notnull
        => m.HasValue ? await f(m.InternalValue) : default;

    #region LINQ aliases

    public static Maybe<B> Select<A, B>(this Maybe<A> m, Func<A, B> f)
        where A : notnull
        where B : notnull
        => m.Map(f);

    public static Maybe<B> SelectMany<A, B>(this Maybe<A> m, Func<A, Maybe<B>> f)
        where A : notnull
        where B : notnull
        => m.FlatMap(f);

    public static Maybe<B> SelectMany<A, X, B>(
        this Maybe<A> m,
        Func<A, Maybe<X>> f,
        Func<A, X, B> compose)
        where A : notnull
        where X : notnull
        where B : notnull
        => m.FlatMap(a => f(a).Map(x => compose(a, x)));

    #endregion
}

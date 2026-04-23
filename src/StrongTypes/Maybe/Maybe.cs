#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>A value that is either <c>Some(T)</c> or <c>None</c>.</summary>
/// <typeparam name="T">The wrapped value type.</typeparam>
/// <remarks>Unwrap with the extension-property pattern: <c>if (maybe.Value is {} v)</c>.</remarks>
[JsonConverter(typeof(MaybeJsonConverterFactory))]
public readonly struct Maybe<T> :
    IEquatable<Maybe<T>>,
    IEquatable<T>,
    IComparable<Maybe<T>>,
    IComparable<T>,
    IEnumerable<T>
    where T : notnull
{
    internal readonly T InternalValue;

    public bool IsSome { get; }
    public bool IsNone => !IsSome;
    public bool HasValue => IsSome;

    private Maybe(T value)
    {
        InternalValue = value;
        IsSome = true;
    }

    public static Maybe<T> Some(T value) => new(value);

    public static readonly Maybe<T> None = default;

    // Lets callers write the untyped `Maybe.None` and have the compiler bind it to
    // any closed `Maybe<T>` from context — same pattern Nullable<T>'s null literal
    // gets from the language.
    public static implicit operator Maybe<T>(MaybeNone _) => default;

    // Lets a bare T flow into a Maybe<T> slot — most useful inside collection
    // expressions like `Maybe<int>[] xs = [1, 2, Maybe.None, 4]`, where each
    // numeric literal converts to Maybe<int>.Some(literal).
    public static implicit operator Maybe<T>(T value) => Some(value);

    #region Match / Map / FlatMap / Where

    public R Match<R>(Func<T, R> ifSome, Func<R> ifNone) =>
        IsSome ? ifSome(InternalValue) : ifNone();

    public void Match(Action<T>? ifSome = null, Action? ifNone = null)
    {
        if (IsSome) ifSome?.Invoke(InternalValue);
        else ifNone?.Invoke();
    }

    public Maybe<B> Map<B>(Func<T, B> f) where B : notnull =>
        IsSome ? Maybe<B>.Some(f(InternalValue)) : default;

    public Maybe<B> FlatMap<B>(Func<T, Maybe<B>> f) where B : notnull =>
        IsSome ? f(InternalValue) : default;

    public Maybe<T> Where(Func<T, bool> predicate) =>
        IsSome && predicate(InternalValue) ? this : default;

    #endregion

    #region Enumeration

    public IEnumerator<T> GetEnumerator()
    {
        if (IsSome) yield return InternalValue;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Equality

    public bool Equals(Maybe<T> other) =>
        IsSome == other.IsSome
        && (IsNone || EqualityComparer<T>.Default.Equals(InternalValue, other.InternalValue));

    public bool Equals(T? other) =>
        IsSome && other is not null && EqualityComparer<T>.Default.Equals(InternalValue, other);

    public override bool Equals(object? obj) =>
        obj is Maybe<T> same && Equals(same);

    public override int GetHashCode() =>
        IsSome ? EqualityComparer<T>.Default.GetHashCode(InternalValue) : 0;

    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);
    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

    public static bool operator ==(Maybe<T> left, T right) => left.Equals(right);
    public static bool operator !=(Maybe<T> left, T right) => !left.Equals(right);
    public static bool operator ==(T left, Maybe<T> right) => right.Equals(left);
    public static bool operator !=(T left, Maybe<T> right) => !right.Equals(left);

    #endregion

    #region Comparison

    public int CompareTo(Maybe<T> other)
    {
        if (IsNone && other.IsNone) return 0;
        if (IsNone) return -1;
        if (other.IsNone) return 1;
        return Comparer<T>.Default.Compare(InternalValue, other.InternalValue);
    }

    public int CompareTo(T? other)
    {
        if (IsNone) return other is null ? 0 : -1;
        return Comparer<T>.Default.Compare(InternalValue, other!);
    }

    public static bool operator <(Maybe<T> left, Maybe<T> right) => left.CompareTo(right) < 0;
    public static bool operator <=(Maybe<T> left, Maybe<T> right) => left.CompareTo(right) <= 0;
    public static bool operator >(Maybe<T> left, Maybe<T> right) => left.CompareTo(right) > 0;
    public static bool operator >=(Maybe<T> left, Maybe<T> right) => left.CompareTo(right) >= 0;

    public static bool operator <(Maybe<T> left, T right) => left.CompareTo(right) < 0;
    public static bool operator <=(Maybe<T> left, T right) => left.CompareTo(right) <= 0;
    public static bool operator >(Maybe<T> left, T right) => left.CompareTo(right) > 0;
    public static bool operator >=(Maybe<T> left, T right) => left.CompareTo(right) >= 0;

    // (T, Maybe<T>) ordering inverts the sign because we delegate to the same
    // CompareTo on the right operand.
    public static bool operator <(T left, Maybe<T> right) => right.CompareTo(left) > 0;
    public static bool operator <=(T left, Maybe<T> right) => right.CompareTo(left) >= 0;
    public static bool operator >(T left, Maybe<T> right) => right.CompareTo(left) < 0;
    public static bool operator >=(T left, Maybe<T> right) => right.CompareTo(left) <= 0;

    #endregion

    public override string ToString() => IsSome ? $"Some({InternalValue})" : "None";
}

/// <summary>Untyped <c>None</c> literal that converts to any <see cref="Maybe{T}"/>.</summary>
public readonly struct MaybeNone;

/// <summary>Factory helpers for <see cref="Maybe{T}"/> with inferred type arguments.</summary>
public static class Maybe
{
    /// <summary>Wraps <paramref name="value"/> as <see cref="Maybe{T}"/>.<c>Some</c>.</summary>
    /// <typeparam name="T">The wrapped value type.</typeparam>
    /// <param name="value">The value to wrap.</param>
    public static Maybe<T> Some<T>(T value) where T : notnull => Maybe<T>.Some(value);

    /// <summary>The untyped <c>None</c> literal.</summary>
    public static MaybeNone None => default;
}

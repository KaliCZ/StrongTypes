#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A value type that either holds a value of <typeparamref name="T"/> or is empty.
/// <para>
/// The typical "has value" check is the extension-property pattern
/// <c>if (maybe.Value is {} v)</c>, which unwraps to the underlying value in a single
/// expression. <c>Value</c> is provided via extension members split between the
/// struct- and class-constrained branches so the returned nullable type is
/// <c>Nullable&lt;T&gt;</c> for value types and <c>T?</c> for reference types.
/// </para>
/// </summary>
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

    public bool HasValue { get; }

    private Maybe(T value)
    {
        InternalValue = value;
        HasValue = true;
    }

    public static Maybe<T> Some(T value) => new(value);

    public static Maybe<T> Empty => default;

    #region Match / Map / FlatMap / Where

    public R Match<R>(Func<T, R> ifSome, Func<R> ifNone) =>
        HasValue ? ifSome(InternalValue) : ifNone();

    public void Match(Action<T>? ifSome = null, Action? ifNone = null)
    {
        if (HasValue) ifSome?.Invoke(InternalValue);
        else ifNone?.Invoke();
    }

    public Maybe<B> Map<B>(Func<T, B> f) where B : notnull =>
        HasValue ? Maybe<B>.Some(f(InternalValue)) : Maybe<B>.Empty;

    public Maybe<B> FlatMap<B>(Func<T, Maybe<B>> f) where B : notnull =>
        HasValue ? f(InternalValue) : Maybe<B>.Empty;

    public Maybe<T> Where(Func<T, bool> predicate) =>
        HasValue && predicate(InternalValue) ? this : Empty;

    #endregion

    #region Enumeration

    public IEnumerator<T> GetEnumerator()
    {
        if (HasValue) yield return InternalValue;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region Equality

    public bool Equals(Maybe<T> other) =>
        HasValue == other.HasValue
        && (!HasValue || EqualityComparer<T>.Default.Equals(InternalValue, other.InternalValue));

    public bool Equals(T? other) =>
        HasValue && other is not null && EqualityComparer<T>.Default.Equals(InternalValue, other);

    // Cross-type equality between Maybe<string> and Maybe<NonEmptyString> mirrors the
    // legacy Option_Old behaviour. A broader scheme covering numeric strong types is
    // tracked separately; see the follow-up GitHub issue referenced in the PR.
    public override bool Equals(object? obj)
    {
        if (obj is Maybe<T> same) return Equals(same);

        if (typeof(T) == typeof(string) && obj is Maybe<NonEmptyString> nesOther)
        {
            if (HasValue != nesOther.HasValue) return false;
            if (!HasValue) return true;
            return string.Equals((string)(object)InternalValue!, nesOther.InternalValue.Value);
        }
        if (typeof(T) == typeof(NonEmptyString) && obj is Maybe<string> strOther)
        {
            if (HasValue != strOther.HasValue) return false;
            if (!HasValue) return true;
            return string.Equals(((NonEmptyString)(object)InternalValue!).Value, strOther.InternalValue);
        }

        return false;
    }

    public override int GetHashCode() =>
        HasValue ? EqualityComparer<T>.Default.GetHashCode(InternalValue) : 0;

    public static bool operator ==(Maybe<T> left, Maybe<T> right) => left.Equals(right);
    public static bool operator !=(Maybe<T> left, Maybe<T> right) => !left.Equals(right);

    #endregion

    #region Comparison

    public int CompareTo(Maybe<T> other)
    {
        if (!HasValue && !other.HasValue) return 0;
        if (!HasValue) return -1;
        if (!other.HasValue) return 1;
        return Comparer<T>.Default.Compare(InternalValue, other.InternalValue);
    }

    public int CompareTo(T? other)
    {
        if (!HasValue) return other is null ? 0 : -1;
        return Comparer<T>.Default.Compare(InternalValue, other!);
    }

    #endregion

    public override string ToString() => HasValue ? $"Some({InternalValue})" : "Empty";
}

/// <summary>
/// Factory helpers for <see cref="Maybe{T}"/> that let callers skip the explicit
/// generic argument (e.g. <c>Maybe.Some("x")</c> instead of <c>Maybe&lt;string&gt;.Some("x")</c>).
/// </summary>
public static class Maybe
{
    public static Maybe<T> Some<T>(T value) where T : notnull => Maybe<T>.Some(value);

    public static Maybe<T> Empty<T>() where T : notnull => Maybe<T>.Empty;
}

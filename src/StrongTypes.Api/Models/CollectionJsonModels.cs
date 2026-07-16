namespace StrongTypes.Api.Models;

/// <summary>
/// Every combination of {<see cref="IEnumerable{T}"/>, <see cref="NonEmptyEnumerable{T}"/>} ×
/// {non-nullable element, nullable element}, for one representative element type per category.
/// </summary>
public sealed record IntCollectionsRequest(
    IEnumerable<int> Enumerable,
    IEnumerable<int?> EnumerableNullable,
    NonEmptyEnumerable<int> NonEmpty,
    NonEmptyEnumerable<int?> NonEmptyNullable);

public sealed record PositiveIntCollectionsRequest(
    IEnumerable<Positive<int>> Enumerable,
    IEnumerable<Positive<int>?> EnumerableNullable,
    NonEmptyEnumerable<Positive<int>> NonEmpty,
    NonEmptyEnumerable<Positive<int>?> NonEmptyNullable);

public sealed record StringCollectionsRequest(
    IEnumerable<string> Enumerable,
    IEnumerable<string?> EnumerableNullable,
    NonEmptyEnumerable<string> NonEmpty,
    NonEmptyEnumerable<string?> NonEmptyNullable);

public sealed record NonEmptyStringCollectionsRequest(
    IEnumerable<NonEmptyString> Enumerable,
    IEnumerable<NonEmptyString?> EnumerableNullable,
    NonEmptyEnumerable<NonEmptyString> NonEmpty,
    NonEmptyEnumerable<NonEmptyString?> NonEmptyNullable);

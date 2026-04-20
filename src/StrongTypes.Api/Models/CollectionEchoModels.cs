#nullable enable

using System.Collections.Generic;

namespace StrongTypes.Api.Models;

/// <summary>
/// Echo-endpoint request DTOs exercising every combination of
/// {<see cref="IEnumerable{T}"/>, <see cref="NonEmptyEnumerable{T}"/>} ×
/// {non-nullable element, nullable element}, for four representative
/// element types (plain value, strong value, plain reference, strong
/// reference). The integration-test suite posts JSON, the controller echoes
/// the DTO back, and the tests assert both the deserialize and serialize
/// halves of the round trip — plus how ASP.NET Core + STJ handle each kind
/// of malformed input.
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

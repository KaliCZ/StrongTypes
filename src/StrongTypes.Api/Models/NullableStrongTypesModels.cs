namespace StrongTypes.Api.Models;

/// <summary>
/// Request DTO whose every property is a nullable strong type. Exists solely so
/// the OpenAPI integration tests can assert that nullable property positions
/// still render the right underlying schema — <c>NonEmptyString?</c> must still
/// describe the string + minLength: 1, <c>Positive&lt;int&gt;?</c> must still
/// describe the integer + exclusive minimum, and <c>NonEmptyEnumerable&lt;T&gt;?</c>
/// must still describe the non-empty array.
/// </summary>
public sealed record NullableStrongTypesRequest(
    NonEmptyString? NullableNonEmptyString,
    Positive<int>? NullablePositiveInt,
    NonEmptyEnumerable<NonEmptyString>? NullableNonEmptyStringArray,
    NonEmptyEnumerable<Positive<int>>? NullableNonEmptyPositiveIntArray);

/// <summary>
/// Request DTO that nests the strong types inside each other so the OpenAPI
/// integration tests can assert every transformer composes — a
/// <c>Maybe&lt;Positive&lt;int&gt;&gt;</c> must not just describe <c>Value</c>
/// as an integer, it must carry the <c>Positive</c> bound through to that
/// inner schema. Same story for <c>Maybe&lt;NonEmptyString&gt;</c> carrying
/// <c>minLength</c>, <c>Maybe&lt;NonEmptyEnumerable&lt;T&gt;&gt;</c> carrying
/// <c>minItems</c>, and <c>NonEmptyEnumerable&lt;Maybe&lt;Positive&lt;int&gt;&gt;&gt;</c>
/// carrying both the array-level and the numeric bound.
/// </summary>
public sealed record NestedStrongTypesRequest(
    Maybe<Positive<int>> MaybePositiveInt,
    Maybe<NonEmptyString> MaybeNonEmptyString,
    Maybe<NonEmptyEnumerable<NonEmptyString>> MaybeNonEmptyStringArray,
    NonEmptyEnumerable<Maybe<Positive<int>>> NonEmptyArrayOfMaybePositiveInt);


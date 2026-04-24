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

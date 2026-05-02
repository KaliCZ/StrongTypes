using System.Collections.Frozen;

namespace StrongTypes.OpenApi.TestApi.Shared;

public sealed record StructEntityRequest<T>(T Value, T? NullableValue) where T : struct;

public sealed record ReferenceEntityRequest<T>(T Value, T? NullableValue) where T : class;

public sealed record StructEntityPatchRequest<T>(T? Value, Maybe<T>? NullableValue) where T : struct;

public sealed record EntityResponse(Guid Id);

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

public sealed record NonEmptyStringCollectionsRequest(
    IEnumerable<NonEmptyString> Enumerable,
    IEnumerable<NonEmptyString?> EnumerableNullable,
    NonEmptyEnumerable<NonEmptyString> NonEmpty,
    NonEmptyEnumerable<NonEmptyString?> NonEmptyNullable);

public sealed record NullableStrongTypesRequest(
    NonEmptyString? NullableNonEmptyString,
    Positive<int>? NullablePositiveInt,
    NonEmptyEnumerable<NonEmptyString>? NullableNonEmptyStringArray,
    NonEmptyEnumerable<Positive<int>>? NullableNonEmptyPositiveIntArray);

public sealed record CollectionShapesRequest(
    IEnumerable<Positive<int>> AsEnumerable,
    IList<Positive<int>> AsIList,
    IReadOnlyList<Positive<int>> AsIReadOnlyList,
    List<Positive<int>> AsList,
    Positive<int>[] AsArray,
    NonEmptyEnumerable<Positive<int>> AsNonEmpty,
    FrozenSet<Positive<int>> AsFrozenSet);

public sealed record DictionaryShapesRequest(
    IDictionary<string, Positive<int>> AsIDictionary,
    Dictionary<int, Positive<int>> AsDictionaryIntKey,
    IReadOnlyDictionary<string, Positive<int>> AsIReadOnlyDictionary,
    FrozenDictionary<string, Positive<int>> AsFrozenDictionary,
    SortedList<string, Positive<int>> AsSortedList);

public sealed record NestedStrongTypesRequest(
    Maybe<Positive<int>> MaybePositiveInt,
    Maybe<NonEmptyString> MaybeNonEmptyString,
    Maybe<NonEmptyEnumerable<NonEmptyString>> MaybeNonEmptyStringArray,
    NonEmptyEnumerable<Maybe<Positive<int>>> NonEmptyArrayOfMaybePositiveInt);

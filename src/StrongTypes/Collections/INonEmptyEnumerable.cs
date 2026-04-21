#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A read-only sequence guaranteed to contain at least one element.
/// </summary>
[JsonConverter(typeof(NonEmptyEnumerableJsonConverterFactory))]
public interface INonEmptyEnumerable<out T> : IReadOnlyList<T>
{
    T Head { get; }

    IReadOnlyList<T> Tail { get; }
}

#nullable enable

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A read-only sequence guaranteed to contain at least one element. The <c>out</c>
/// variance lets <c>INonEmptyEnumerable&lt;Derived&gt;</c> flow into slots typed
/// as <c>INonEmptyEnumerable&lt;Base&gt;</c>.
/// </summary>
/// <remarks>
/// Round-trips through <c>System.Text.Json</c> the same way
/// <see cref="NonEmptyEnumerable{T}"/> does — the shared
/// <see cref="NonEmptyEnumerableJsonConverterFactory"/> matches both the interface and
/// the class. Deserialization builds a <see cref="NonEmptyEnumerable{T}"/> and returns it
/// as the interface.
/// </remarks>
[JsonConverter(typeof(NonEmptyEnumerableJsonConverterFactory))]
public interface INonEmptyEnumerable<out T> : IReadOnlyList<T>
{
    T Head { get; }

    IReadOnlyList<T> Tail { get; }
}

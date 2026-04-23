using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>A read-only sequence guaranteed to contain at least one element.</summary>
/// <typeparam name="T">The element type.</typeparam>
[JsonConverter(typeof(NonEmptyEnumerableJsonConverterFactory))]
public interface INonEmptyEnumerable<out T> : IReadOnlyList<T>
{
    /// <summary>The first element.</summary>
    T Head { get; }

    /// <summary>The elements after <see cref="Head"/>. May be empty.</summary>
    IReadOnlyList<T> Tail { get; }
}

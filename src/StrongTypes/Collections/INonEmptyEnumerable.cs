#nullable enable

using System.Collections.Generic;

namespace StrongTypes;

/// <summary>
/// A read-only list guaranteed to contain at least one element. The <c>out</c>
/// variance lets <c>INonEmptyEnumerable&lt;Derived&gt;</c> flow into slots typed
/// as <c>INonEmptyEnumerable&lt;Base&gt;</c>.
/// </summary>
/// <remarks>
/// For JSON round-tripping, type properties as the concrete <see cref="NonEmptyEnumerable{T}"/>.
/// The JSON converter is wired to the class rather than the interface, since <c>System.Text.Json</c>
/// requires a converter whose generic argument exactly matches the declared property type.
/// </remarks>
public interface INonEmptyEnumerable<out T> : IReadOnlyList<T>
{
    T Head { get; }

    IReadOnlyList<T> Tail { get; }
}

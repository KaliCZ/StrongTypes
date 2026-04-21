#nullable enable

using System.Diagnostics;

namespace StrongTypes;

internal sealed class NonEmptyEnumerableDebugView<T>(NonEmptyEnumerable<T> source)
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => source.DebugArray;
}

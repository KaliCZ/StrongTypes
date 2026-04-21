#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace StrongTypes;

public sealed partial class NonEmptyEnumerable<T>
{
    public struct Enumerator : IEnumerator<T>
    {
        private readonly T[] _values;
        private int _index;

        internal Enumerator(T[] values)
        {
            _values = values;
            _index = -1;
        }

        public readonly T Current => _values[_index];
        readonly object? IEnumerator.Current => Current;

        public bool MoveNext() => ++_index < _values.Length;

        public void Reset() => _index = -1;

        public readonly void Dispose() { }
    }
}

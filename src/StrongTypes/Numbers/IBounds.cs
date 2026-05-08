using System.Numerics;

namespace StrongTypes;

/// <summary>Witness type that supplies a closed range <c>[Min, Max]</c> for <see cref="BoundedInt{TBounds}"/> (and future bounded numeric wrappers).</summary>
/// <typeparam name="T">The underlying numeric type.</typeparam>
/// <remarks>Implement on a small <c>readonly struct</c> with <c>=&gt; literal</c> getters; the bounds travel with the type and the JIT can inline them.</remarks>
public interface IBounds<T> where T : INumber<T>
{
    static abstract T Min { get; }
    static abstract T Max { get; }
}

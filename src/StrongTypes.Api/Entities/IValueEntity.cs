namespace StrongTypes.Api.Entities;

/// <summary>
/// Value-type counterpart of <see cref="IEntity{TSelf, T}"/>. Identical shape
/// but with <c>where T : struct</c> so that <c>T?</c> maps to
/// <see cref="System.Nullable{T}"/> instead of a nullable reference type.
/// </summary>
public interface IValueEntity<TSelf, T>
    where TSelf : IValueEntity<TSelf, T>
    where T : struct
{
    Guid Id { get; }
    T Value { get; set; }
    T? NullableValue { get; set; }
    void Update(T value, T? nullableValue);
    static abstract TSelf Create(T value, T? nullableValue);
}

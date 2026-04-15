namespace StrongTypes.Api.Entities;

/// <summary>
/// Shape every integration-test entity follows: an identifier plus a required
/// value and an optional value of the same strong type <typeparamref name="T"/>.
/// The <c>Create</c> static factory lets generic controllers construct concrete
/// entities without each one having to override a factory method.
/// </summary>
public interface IEntity<TSelf, T>
    where TSelf : IEntity<TSelf, T>
    where T : class
{
    Guid Id { get; }
    T Value { get; set; }
    T? NullableValue { get; set; }
    void Update(T value, T? nullableValue);
    static abstract TSelf Create(T value, T? nullableValue);
}

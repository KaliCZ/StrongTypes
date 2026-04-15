namespace StrongTypes.Api.Entities;

/// <summary>
/// Shape every integration-test entity follows: an identifier plus a required
/// value and an optional value of the same strong type <typeparamref name="T"/>.
/// Generic helpers on controllers and tests use this interface so a new strong
/// type only needs a new concrete entity + controller, not new plumbing.
/// </summary>
public interface IValuedEntity<T> where T : class
{
    Guid Id { get; }
    T Value { get; set; }
    T? NullableValue { get; set; }
    void Update(T value, T? nullableValue);
}

namespace StrongTypes.Api.Entities;

public interface IEntity
{
    Guid Id { get; }
}

/// <summary>
/// Shape every integration-test entity follows: an identifier plus a required
/// <typeparamref name="T"/> value and an optional <typeparamref name="TNullable"/>
/// value. A single interface covers both reference and value strong types by
/// letting callers choose <typeparamref name="TNullable"/> (typically <c>T?</c>).
/// </summary>
public interface IEntity<TSelf, T, TNullable> : IEntity
    where TSelf : IEntity<TSelf, T, TNullable>
    where T : notnull
{
    T Value { get; set; }
    TNullable NullableValue { get; set; }

    void Update(T value, TNullable nullableValue)
    {
        Value = value;
        NullableValue = nullableValue;
    }

    static abstract TSelf Create(T value, TNullable nullableValue);
}

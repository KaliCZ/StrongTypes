namespace StrongTypes.Api.Entities;

public interface IEntity
{
    Guid Id { get; }
}

/// <summary>TNullable is its own parameter so one interface spans value and reference wrappers — each supplies its form of T?.</summary>
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

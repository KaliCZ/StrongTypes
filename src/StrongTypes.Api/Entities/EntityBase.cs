namespace StrongTypes.Api.Entities;

public abstract class EntityBase<TSelf, T, TNullable> : IEntity<TSelf, T, TNullable>
    where TSelf : EntityBase<TSelf, T, TNullable>, new()
    where T : notnull
{
    public Guid Id { get; set; }
    public T Value { get; set; } = default!;
    public TNullable NullableValue { get; set; } = default!;

    /// <summary>Shadows the interface's default Update so callers don't have to cast to the interface.</summary>
    public void Update(T value, TNullable nullableValue)
    {
        Value = value;
        NullableValue = nullableValue;
    }

    public static TSelf Create(T value, TNullable nullableValue)
    {
        var entity = new TSelf { Id = Guid.NewGuid() };
        entity.Update(value, nullableValue);
        return entity;
    }
}

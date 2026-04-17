namespace StrongTypes.Api.Entities;

/// <summary>
/// Shared state and factory for concrete <see cref="IEntity{TSelf, T, TNullable}"/>
/// implementations. Concrete entities just declare their generic arguments; this
/// base supplies the <c>Id</c>/<c>Value</c>/<c>NullableValue</c> storage and the
/// static <c>Create</c> required by the interface.
/// </summary>
public abstract class EntityBase<TSelf, T, TNullable> : IEntity<TSelf, T, TNullable>
    where TSelf : EntityBase<TSelf, T, TNullable>, new()
    where T : notnull
{
    public Guid Id { get; set; }
    public T Value { get; set; } = default!;
    public TNullable NullableValue { get; set; } = default!;

    public static TSelf Create(T value, TNullable nullableValue)
    {
        var entity = new TSelf { Id = Guid.NewGuid() };
        ((IEntity<TSelf, T, TNullable>)entity).Update(value, nullableValue);
        return entity;
    }
}

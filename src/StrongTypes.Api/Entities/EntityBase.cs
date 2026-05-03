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

    /// <summary>
    /// Public surface for the interface's default <c>Update</c>: lets callers
    /// invoke <c>entity.Update(...)</c> directly on a concrete type without
    /// casting through <see cref="IEntity{TSelf, T, TNullable}"/>.
    /// </summary>
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

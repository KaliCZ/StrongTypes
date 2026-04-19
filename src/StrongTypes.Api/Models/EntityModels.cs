using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Models;

public record EntityRequest<T, TNullable>(T Value, TNullable NullableValue) where T : notnull;

/// <summary>
/// PATCH request body. Each field is independently "sent or not":
/// <list type="bullet">
///   <item><description><c>Value</c>: <c>null</c> (or absent) ⇒ do not update; a value ⇒ update.
///     The entity's Value is non-nullable so it can only be updated, never cleared.</description></item>
///   <item><description><c>NullableValue</c>: <c>null</c> (or absent) ⇒ do not update;
///     <c>Maybe</c> empty (<c>{}</c> or <c>{"Value":null}</c>) ⇒ clear to null on the entity;
///     <c>{"Value":x}</c> ⇒ set to x.</description></item>
/// </list>
/// </summary>
public record EntityPatchRequest<T, TNullable>(TNullable Value, Maybe<T>? NullableValue)
    where T : notnull;

public record EntityResponse(Guid Id);

public record EntityDto<T, TNullable>(Guid Id, T Value, TNullable NullableValue) where T : notnull
{
    public static EntityDto<T, TNullable> From<TEntity>(TEntity entity)
        where TEntity : IEntity<TEntity, T, TNullable>
        => new(entity.Id, entity.Value, entity.NullableValue);
}

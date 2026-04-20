using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Models;

public record StructEntityRequest<T>(T Value, T? NullableValue) where T : struct;

public record ReferenceEntityRequest<T>(T Value, T? NullableValue) where T : class;

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
public record StructEntityPatchRequest<T>(T? Value, Maybe<T>? NullableValue) where T : struct;

/// <inheritdoc cref="StructEntityPatchRequest{T}"/>
public record ReferenceEntityPatchRequest<T>(T? Value, Maybe<T>? NullableValue) where T : class;

public record EntityResponse(Guid Id);

public record StructEntityDto<T>(Guid Id, T Value, T? NullableValue) where T : struct
{
    public static StructEntityDto<T> From<TEntity>(TEntity entity)
        where TEntity : IEntity<TEntity, T, T?>
        => new(entity.Id, entity.Value, entity.NullableValue);
}

public record ReferenceEntityDto<T>(Guid Id, T Value, T? NullableValue) where T : class
{
    public static ReferenceEntityDto<T> From<TEntity>(TEntity entity)
        where TEntity : IEntity<TEntity, T, T?>
        => new(entity.Id, entity.Value, entity.NullableValue);
}

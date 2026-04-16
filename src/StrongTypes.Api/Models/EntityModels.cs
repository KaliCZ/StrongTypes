using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Models;

public record NonNullableRequest<T>(T Value, T NullableValue) where T : class;
public record NullableRequest<T>(T Value, T? NullableValue) where T : class;

public record EntityResponse(Guid Id);

public record EntityDto<T>(Guid Id, T Value, T? NullableValue) where T : class
{
    public static EntityDto<T> From<TEntity>(TEntity entity) where TEntity : IEntity<TEntity, T>
        => new(entity.Id, entity.Value, entity.NullableValue);
}

using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Models;

public record NonNullableRequest<T>(T Value, T NullableValue) where T : notnull;
public record NullableRequest<T, TNullable>(T Value, TNullable NullableValue) where T : notnull;

public record EntityResponse(Guid Id);

public record EntityDto<T, TNullable>(Guid Id, T Value, TNullable NullableValue) where T : notnull
{
    public static EntityDto<T, TNullable> From<TEntity>(TEntity entity)
        where TEntity : IEntity<TEntity, T, TNullable>
        => new(entity.Id, entity.Value, entity.NullableValue);
}

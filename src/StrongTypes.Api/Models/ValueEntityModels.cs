using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Models;

public record NonNullableValueRequest<T>(T Value, T NullableValue) where T : struct;
public record NullableValueRequest<T>(T Value, T? NullableValue) where T : struct;

public record ValueEntityDto<T>(Guid Id, T Value, T? NullableValue) where T : struct
{
    public static ValueEntityDto<T> From<TEntity>(TEntity entity) where TEntity : IValueEntity<TEntity, T>
        => new(entity.Id, entity.Value, entity.NullableValue);
}

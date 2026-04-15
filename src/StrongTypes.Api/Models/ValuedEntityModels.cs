using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Models;

public record NonNullableValuedRequest<T>(T Value, T NullableValue) where T : class;
public record NullableValuedRequest<T>(T Value, T? NullableValue) where T : class;

public record EntityResponse(Guid Id);

public record ValuedEntityDto<T>(Guid Id, T Value, T? NullableValue) where T : class
{
    public static ValuedEntityDto<T> From<TEntity>(TEntity entity) where TEntity : IValuedEntity<T>
        => new(entity.Id, entity.Value, entity.NullableValue);
}

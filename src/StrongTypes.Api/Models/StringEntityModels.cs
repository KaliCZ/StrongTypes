using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Models;

public record CreateNonNullableRequest(NonEmptyString Value, NonEmptyString NullableValue);
public record CreateNullableRequest(NonEmptyString Value, NonEmptyString? NullableValue);
public record UpdateNonNullableRequest(NonEmptyString Value, NonEmptyString NullableValue);
public record UpdateNullableRequest(NonEmptyString Value, NonEmptyString? NullableValue);
public record StringEntityResponse(Guid Id);

public record StringEntityDto(Guid Id, NonEmptyString Value, NonEmptyString? NullableValue)
{
    public StringEntityDto(StringEntity entity) : this(entity.Id, entity.Value, entity.NullableValue) { }
}

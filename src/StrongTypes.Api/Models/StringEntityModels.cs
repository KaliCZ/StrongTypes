using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Models;

public record CreateNonNullableRequest(string Value, string NullableValue);
public record CreateNullableRequest(string Value, string? NullableValue);
public record UpdateNonNullableRequest(string Value, string NullableValue);
public record UpdateNullableRequest(string Value, string? NullableValue);
public record StringEntityResponse(Guid Id);

public record StringEntityDto(Guid Id, string Value, string? NullableValue)
{
    public StringEntityDto(StringEntity entity) : this(entity.Id, entity.Value, entity.NullableValue) { }
}

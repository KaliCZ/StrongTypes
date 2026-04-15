namespace StrongTypes.Api.Models;

public record CreateNonNullableRequest(string Value, string NullableValue);
public record CreateNullableRequest(string Value, string? NullableValue);
public record UpdateNonNullableRequest(string Value, string NullableValue);
public record UpdateNullableRequest(string Value, string? NullableValue);
public record StringEntityResponse(Guid Id);

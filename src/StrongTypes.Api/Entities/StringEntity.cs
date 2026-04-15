namespace StrongTypes.Api.Entities;

public class StringEntity
{
    public Guid Id { get; set; }
    public string Value { get; set; } = null!;
    public string? NullableValue { get; set; }
}

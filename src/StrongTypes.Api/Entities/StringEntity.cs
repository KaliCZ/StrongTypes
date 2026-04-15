namespace StrongTypes.Api.Entities;

public class StringEntity
{
    // Required by EF Core for materialization from query results.
    private StringEntity() { }

    public StringEntity(string value, string? nullableValue)
    {
        Id = Guid.NewGuid();
        Value = value;
        NullableValue = nullableValue;
    }

    public Guid Id { get; set; }
    public string Value { get; set; } = null!;
    public string? NullableValue { get; set; }

    public void Update(string value, string? nullableValue)
    {
        Value = value;
        NullableValue = nullableValue;
    }
}

namespace StrongTypes.Api.Entities;

public class StringEntity
{
    // Required by EF Core for materialization from query results.
    private StringEntity() { }

    public StringEntity(NonEmptyString value, NonEmptyString? nullableValue)
    {
        Id = Guid.NewGuid();
        Value = value;
        NullableValue = nullableValue;
    }

    public Guid Id { get; set; }
    public NonEmptyString Value { get; set; } = null!;
    public NonEmptyString? NullableValue { get; set; }

    public void Update(NonEmptyString value, NonEmptyString? nullableValue)
    {
        Value = value;
        NullableValue = nullableValue;
    }
}

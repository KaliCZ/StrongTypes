namespace StrongTypes.Api.Entities;

public sealed class NonEmptyStringEntity : IEntity<NonEmptyStringEntity, NonEmptyString>
{
    // Required by EF Core for materialization from query results.
    private NonEmptyStringEntity() { }

    private NonEmptyStringEntity(NonEmptyString value, NonEmptyString? nullableValue)
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

    public static NonEmptyStringEntity Create(NonEmptyString value, NonEmptyString? nullableValue) =>
        new(value, nullableValue);
}

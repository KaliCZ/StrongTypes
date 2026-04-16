namespace StrongTypes.Api.Entities;

public sealed class NonNegativeIntEntity : IValueEntity<NonNegativeIntEntity, NonNegative<int>>
{
    private NonNegativeIntEntity() { }

    private NonNegativeIntEntity(NonNegative<int> value, NonNegative<int>? nullableValue)
    {
        Id = Guid.NewGuid();
        Value = value;
        NullableValue = nullableValue;
    }

    public Guid Id { get; set; }
    public NonNegative<int> Value { get; set; }
    public NonNegative<int>? NullableValue { get; set; }

    public void Update(NonNegative<int> value, NonNegative<int>? nullableValue)
    {
        Value = value;
        NullableValue = nullableValue;
    }

    public static NonNegativeIntEntity Create(NonNegative<int> value, NonNegative<int>? nullableValue) =>
        new(value, nullableValue);
}

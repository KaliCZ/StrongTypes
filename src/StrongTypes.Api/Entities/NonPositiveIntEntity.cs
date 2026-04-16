namespace StrongTypes.Api.Entities;

public sealed class NonPositiveIntEntity : IValueEntity<NonPositiveIntEntity, NonPositive<int>>
{
    private NonPositiveIntEntity() { }

    private NonPositiveIntEntity(NonPositive<int> value, NonPositive<int>? nullableValue)
    {
        Id = Guid.NewGuid();
        Value = value;
        NullableValue = nullableValue;
    }

    public Guid Id { get; set; }
    public NonPositive<int> Value { get; set; }
    public NonPositive<int>? NullableValue { get; set; }

    public void Update(NonPositive<int> value, NonPositive<int>? nullableValue)
    {
        Value = value;
        NullableValue = nullableValue;
    }

    public static NonPositiveIntEntity Create(NonPositive<int> value, NonPositive<int>? nullableValue) =>
        new(value, nullableValue);
}

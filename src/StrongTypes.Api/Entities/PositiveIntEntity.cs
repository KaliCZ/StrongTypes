namespace StrongTypes.Api.Entities;

public sealed class PositiveIntEntity : IValueEntity<PositiveIntEntity, Positive<int>>
{
    private PositiveIntEntity() { }

    private PositiveIntEntity(Positive<int> value, Positive<int>? nullableValue)
    {
        Id = Guid.NewGuid();
        Value = value;
        NullableValue = nullableValue;
    }

    public Guid Id { get; set; }
    public Positive<int> Value { get; set; }
    public Positive<int>? NullableValue { get; set; }

    public void Update(Positive<int> value, Positive<int>? nullableValue)
    {
        Value = value;
        NullableValue = nullableValue;
    }

    public static PositiveIntEntity Create(Positive<int> value, Positive<int>? nullableValue) =>
        new(value, nullableValue);
}

namespace StrongTypes.Api.Entities;

public sealed class NegativeIntEntity : IValueEntity<NegativeIntEntity, Negative<int>>
{
    private NegativeIntEntity() { }

    private NegativeIntEntity(Negative<int> value, Negative<int>? nullableValue)
    {
        Id = Guid.NewGuid();
        Value = value;
        NullableValue = nullableValue;
    }

    public Guid Id { get; set; }
    public Negative<int> Value { get; set; }
    public Negative<int>? NullableValue { get; set; }

    public void Update(Negative<int> value, Negative<int>? nullableValue)
    {
        Value = value;
        NullableValue = nullableValue;
    }

    public static NegativeIntEntity Create(Negative<int> value, Negative<int>? nullableValue) =>
        new(value, nullableValue);
}

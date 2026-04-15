using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// The Unit type. It has only one instance.
/// </summary>
[JsonConverter(typeof(UnitJsonConverter))]
public sealed class Unit
{
    private Unit()
    {
    }

    /// <summary>
    /// The only instance of the Unit type.
    /// </summary>
    public static Unit Value { get; } = new Unit();

    public override int GetHashCode()
    {
        return 42;
    }
    public override bool Equals(object obj)
    {
        return this == obj;
    }
    public override string ToString()
    {
        return "()";
    }
}
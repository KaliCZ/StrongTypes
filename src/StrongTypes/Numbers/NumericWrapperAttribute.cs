using System;

namespace StrongTypes;

/// <summary>Marks a <c>partial struct</c> as a numeric strong-type wrapper for the source generator.</summary>
/// <remarks>The target must declare a public <c>Value</c> instance property and a public static <c>TryCreate</c> returning <c>Self?</c>.</remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class NumericWrapperAttribute : Attribute
{
    /// <summary>Short adjective phrase embedded in the <c>Create</c> throw message, e.g. <c>"positive"</c>.</summary>
    public string InvariantDescription { get; set; } = "valid";

    /// <summary>When <c>true</c>, the generator emits a <c>Sum</c> extension on <c>IEnumerable&lt;Self&gt;</c>. Only safe when the invariant is closed under addition.</summary>
    public bool GenerateSum { get; set; }
}

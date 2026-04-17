#nullable enable

using System;

namespace StrongTypes;

/// <summary>
/// Marks a <c>partial struct</c> as a numeric strong-type wrapper. The source
/// generator fills in the standard equality, comparison, operator, conversion,
/// and <c>Create</c>-factory boilerplate, as well as LINQ-style extension methods
/// on <c>IEnumerable&lt;Self&gt;</c>.
/// </summary>
/// <remarks>
/// The target must declare:
/// <list type="bullet">
/// <item>a public <c>Value</c> instance property exposing the underlying numeric
/// value;</item>
/// <item>a public static <c>TryCreate</c> method returning <c>Self?</c> that
/// encodes the validation rule.</item>
/// </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class NumericWrapperAttribute : Attribute
{
    /// <summary>
    /// Short adjective phrase used in the <c>Create</c>-throws message, e.g.
    /// <c>"positive"</c> or <c>"non-negative"</c>. The final message reads
    /// <c>$"Value must be {InvariantDescription}, but was '{value}'."</c>.
    /// </summary>
    public string InvariantDescription { get; set; } = "valid";

    /// <summary>
    /// Emit a <c>Sum</c> extension on <c>IEnumerable&lt;Self&gt;</c>. Only set
    /// to <c>true</c> when the wrapper's invariant is closed under addition —
    /// e.g. <c>NonNegative</c> yes, bounded ranges no.
    /// </summary>
    public bool GenerateSum { get; set; }
}

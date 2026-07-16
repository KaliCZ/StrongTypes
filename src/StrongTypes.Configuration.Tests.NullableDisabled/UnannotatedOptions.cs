namespace StrongTypes.Configuration.Tests.NullableDisabled;

/// <summary>An options class from an assembly with no nullable annotations: <c>Name</c> declares no intent either way, while <c>MaxRetries</c> and <c>Score</c> still differ in the type itself.</summary>
public sealed class UnannotatedOptions
{
    public NonEmptyString Name { get; set; }
    public Positive<int> MaxRetries { get; set; }
    public Positive<int>? Score { get; set; }
}

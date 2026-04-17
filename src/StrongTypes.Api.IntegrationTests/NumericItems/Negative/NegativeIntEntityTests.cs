using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.Negative;

[Collection(IntegrationTestCollection.Name)]
public sealed class NegativeIntEntityTests(TestWebApplicationFactory factory)
    : NumericEntityTests<NegativeIntEntity, Negative<int>>(factory)
{
    protected override string RoutePrefix => "negative-int-entities";
    protected override Negative<int> Create(int raw) => Negative<int>.Create(raw);
    protected override (int, int) SeedValid => (-5, -42);
    protected override (int, int) SeedValidUpdate => (-100, -200);

    public static TheoryData<int, int> ValidInputs => new()
    {
        { -5, -42 },
        { -10, -7 },
        { -1, int.MinValue },
    };

    public static TheoryData<int?, int?> InvalidInputs => new()
    {
        // Null Value
        { null, -1 },
        { null, null },
        // Invalid Value (zero or positive) with valid nullable
        { 0, -1 },
        { 1, -1 },
        // Valid Value with invalid nullable
        { -1, 0 },
        { -1, 1 },
    };
}

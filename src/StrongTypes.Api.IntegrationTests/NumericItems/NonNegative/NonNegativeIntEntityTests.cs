using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.NonNegative;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonNegativeIntEntityTests(TestWebApplicationFactory factory)
    : NumericEntityTests<NonNegativeIntEntity, NonNegative<int>>(factory)
{
    protected override string RoutePrefix => "non-negative-int-entities";
    protected override NonNegative<int> Create(int raw) => NonNegative<int>.Create(raw);
    protected override (int, int) SeedValid => (0, 42);
    protected override (int, int) SeedValidUpdate => (100, 200);

    public static TheoryData<int, int> ValidInputs => new()
    {
        { 0, 42 },
        { 5, 10 },
        { 1, int.MaxValue },
    };

    public static TheoryData<int?, int?> InvalidInputs => new()
    {
        // Null Value
        { null, 0 },
        { null, null },
        // Invalid Value (negative) with valid nullable
        { -1, 0 },
        { -100, 0 },
        // Valid Value with invalid nullable
        { 0, -1 },
        { 5, -100 },
    };
}

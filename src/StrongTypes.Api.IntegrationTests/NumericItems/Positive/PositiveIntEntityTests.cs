using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.Positive;

[Collection(IntegrationTestCollection.Name)]
public sealed class PositiveIntEntityTests(TestWebApplicationFactory factory)
    : NumericEntityTests<PositiveIntEntity, Positive<int>>(factory)
{
    protected override string RoutePrefix => "positive-int-entities";
    protected override Positive<int> Create(int raw) => Positive<int>.Create(raw);
    protected override (int, int) SeedValid => (5, 42);
    protected override (int, int) SeedValidUpdate => (100, 200);

    public static TheoryData<int, int> ValidInputs => new()
    {
        { 5, 42 },
        { 10, 7 },
        { 1, int.MaxValue },
    };

    public static TheoryData<int?, int?> InvalidInputs => new()
    {
        // Null Value
        { null, 1 },
        { null, null },
        // Invalid Value (non-positive) with valid nullable
        { 0, 1 },
        { -1, 1 },
        // Valid Value with invalid nullable
        { 1, 0 },
        { 1, -1 },
    };
}

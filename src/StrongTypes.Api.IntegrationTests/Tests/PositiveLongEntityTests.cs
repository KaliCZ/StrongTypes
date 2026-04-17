using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class PositiveLongEntityTests(TestWebApplicationFactory factory)
    : EntityTests<PositiveLongEntityTests, PositiveLongEntity, Positive<long>, Positive<long>?, long>(factory),
      IEntityTestData<long>
{
    protected override string RoutePrefix => "positive-long-entities";
    protected override Positive<long> Create(long raw) => Positive<long>.Create(raw);
    protected override long FirstValid => 5L;
    protected override long UpdatedValid => 100L;

    public static TheoryData<long> ValidInputs => new() { 5L, 42L, 10L, 1L, long.MaxValue };
    public static TheoryData<long> InvalidInputs => new() { 0L, -1L, -100L, long.MinValue };
}

using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonPositiveLongEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NonPositiveLongEntityTests, NonPositiveLongEntity, NonPositive<long>, NonPositive<long>?, long>(factory),
      IEntityTestData<long>
{
    protected override string RoutePrefix => "non-positive-long-entities";
    protected override NonPositive<long> Create(long raw) => NonPositive<long>.Create(raw);
    protected override long FirstValid => 0L;
    protected override long UpdatedValid => -100L;

    public static TheoryData<long> ValidInputs => new() { 0L, -5L, -42L, -10L, long.MinValue };
    public static TheoryData<long> InvalidInputs => new() { 1L, 100L, long.MaxValue };
}

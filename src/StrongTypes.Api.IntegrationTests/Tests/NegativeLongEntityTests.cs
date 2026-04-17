using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NegativeLongEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NegativeLongEntityTests, NegativeLongEntity, Negative<long>, Negative<long>?, long>(factory),
      IEntityTestData<long>
{
    protected override string RoutePrefix => "negative-long-entities";
    protected override Negative<long> Create(long raw) => Negative<long>.Create(raw);
    protected override long FirstValid => -5L;
    protected override long UpdatedValid => -100L;

    public static TheoryData<long> ValidInputs => new() { -5L, -42L, -10L, -1L, long.MinValue };
    public static TheoryData<long> InvalidInputs => new() { 0L, 1L, 100L, long.MaxValue };
}

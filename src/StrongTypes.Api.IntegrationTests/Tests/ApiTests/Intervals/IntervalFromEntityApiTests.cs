using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalFromEntityApiTests(TestWebApplicationFactory factory)
    : IntervalEntityTests<IntervalFromEntity, IntervalFrom<int>>(factory)
{
    protected override string RoutePrefix => "interval-from-entities";

    protected override object ValidBody => new { Start = 1, End = 10 };
    protected override IntervalFrom<int> ValidValue => IntervalFrom<int>.Create(1, 10);

    // Open upper bound: Start required, End absent.
    protected override object UpdatedBody => new { Start = 5, End = (int?)null };
    protected override IntervalFrom<int> UpdatedValue => IntervalFrom<int>.Create(5, null);

    protected override object StartAfterEndBody => new { Start = 10, End = 1 };

    // Start is required; a null Start is a 400.
    protected override object? NullRequiredEndpointBody => new { Start = (int?)null, End = 5 };

    // ...and so is omitting Start entirely. (Omitting End is valid — it's optional.)
    protected override object? OmittedRequiredEndpointBody => new { End = 10 };
}

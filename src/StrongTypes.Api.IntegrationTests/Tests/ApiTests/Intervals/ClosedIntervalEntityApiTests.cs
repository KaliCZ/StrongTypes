using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class ClosedIntervalEntityApiTests(TestWebApplicationFactory factory)
    : IntervalEntityTests<ClosedIntervalEntity, ClosedInterval<int>>(factory)
{
    protected override string RoutePrefix => "closed-interval-entities";

    protected override object ValidBody => new { Start = 1, End = 10 };
    protected override ClosedInterval<int> ValidValue => ClosedInterval<int>.Create(1, 10);

    protected override object UpdatedBody => new { Start = 20, End = 30 };
    protected override ClosedInterval<int> UpdatedValue => ClosedInterval<int>.Create(20, 30);

    protected override object StartAfterEndBody => new { Start = 10, End = 1 };

    // Both endpoints are required; a null endpoint is a 400.
    protected override object? NullRequiredEndpointBody => new { Start = (int?)null, End = 5 };

    // ...and so is omitting one entirely (here End).
    protected override object? OmittedRequiredEndpointBody => new { Start = 1 };
}

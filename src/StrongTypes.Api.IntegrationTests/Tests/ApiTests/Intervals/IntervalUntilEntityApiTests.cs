using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class IntervalUntilEntityApiTests(TestWebApplicationFactory factory)
    : IntervalEntityTests<IntervalUntilEntity, IntervalUntil<int>>(factory)
{
    protected override string RoutePrefix => "interval-until-entities";

    protected override object ValidBody => new { Start = 1, End = 10 };
    protected override IntervalUntil<int> ValidValue => IntervalUntil<int>.Create(1, 10);

    // Open lower bound: End required, Start absent.
    protected override object UpdatedBody => new { Start = (int?)null, End = 5 };
    protected override IntervalUntil<int> UpdatedValue => IntervalUntil<int>.Create(null, 5);

    protected override object StartAfterEndBody => new { Start = 10, End = 1 };

    // End is required; a null End is a 400.
    protected override object? NullRequiredEndpointBody => new { Start = 5, End = (int?)null };
}

using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// The object-wire adapter over <see cref="EntityCrudTestsBase{TEntity, T, TNullable}"/>:
/// interval strong types serialize as a JSON object (<c>{ "start": …, "end": … }</c>) rather
/// than a scalar. It supplies the GET read assertion for the shared create / get / update /
/// PATCH suite and adds the interval-only invalid-payload cases (<c>Start &gt; End</c> and a
/// missing required endpoint). Concrete variants supply the wire bodies and their strong-typed
/// forms.
/// </summary>
public abstract class IntervalEntityTests<TEntity, TInterval>(TestWebApplicationFactory factory)
    : EntityCrudTestsBase<TEntity, TInterval, TInterval?>(factory)
    where TEntity : class, IEntity<TEntity, TInterval, TInterval?>
    where TInterval : struct
{
    private static readonly JsonSerializerOptions WireJson = new(JsonSerializerDefaults.Web);

    /// <summary>A wire body whose endpoints violate <c>Start &lt;= End</c>. Every variant has one.</summary>
    protected abstract object StartAfterEndBody { get; }

    /// <summary>
    /// A wire body that sends <c>null</c> for an endpoint the variant requires, or
    /// <see langword="null"/> when the variant has no required endpoint (<see cref="Interval{T}"/>).
    /// </summary>
    protected virtual object? NullRequiredEndpointBody => null;

    /// <summary>
    /// A wire body that omits a required endpoint key entirely (vs. sending it as <c>null</c>),
    /// or <see langword="null"/> when the variant has no required endpoint (<see cref="Interval{T}"/>).
    /// </summary>
    protected virtual object? OmittedRequiredEndpointBody => null;

    protected override void AssertJsonIsValidValue(JsonElement element) =>
        Assert.Equal(ValidValue, element.Deserialize<TInterval>(WireJson));

    // ── Invalid payloads ─────────────────────────────────────────────────

    [Fact]
    public async Task StartAfterEnd_InValue_ReturnsBadRequestKeyedByValuePath()
    {
        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = StartAfterEndBody, nullableValue = ValidBody }, Ct);
        var errors = await AssertValidationProblem(response);
        Assert.True(errors.TryGetProperty("$.value", out var messages));
        Assert.Contains("less than or equal", string.Join(" ", messages.EnumerateArray().Select(m => m.GetString())));
    }

    [Fact]
    public async Task StartAfterEnd_InNullableValue_ReturnsBadRequestKeyedByNullableValuePath()
    {
        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = ValidBody, nullableValue = StartAfterEndBody }, Ct);
        var errors = await AssertValidationProblem(response);
        Assert.True(errors.TryGetProperty("$.nullableValue", out _));
    }

    [Fact]
    public async Task OmittedRequiredEndpoint_InValue_ReturnsBadRequestKeyedByValuePath()
    {
        Assert.SkipWhen(OmittedRequiredEndpointBody is null, "Variant has no required endpoint.");

        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = OmittedRequiredEndpointBody, nullableValue = ValidBody }, Ct);
        var errors = await AssertValidationProblem(response);
        Assert.True(errors.TryGetProperty("$.value", out var messages));
        Assert.Contains("requires the", string.Join(" ", messages.EnumerateArray().Select(m => m.GetString())));
    }

    [Fact]
    public async Task NullRequiredEndpoint_ReturnsBadRequest()
    {
        Assert.SkipWhen(NullRequiredEndpointBody is null, "Variant has no required endpoint.");

        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = NullRequiredEndpointBody, nullableValue = ValidBody }, Ct);
        var errors = await AssertValidationProblem(response);
        Assert.True(errors.TryGetProperty("$.value", out _));
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Compile-time contract every scalar entity test class must satisfy. <c>static abstract</c>
/// members force each concrete subclass to supply the <see cref="TheoryData{T}"/> lists — a
/// missing one is a build error, not a silently skipped test. xUnit resolves the actual
/// members by reflection on the concrete type at discovery time.
/// </summary>
public interface IEntityTestData<TWire>
{
    static abstract TheoryData<TWire> ValidInputs { get; }
    static abstract TheoryData<TWire> InvalidInputs { get; }
}

/// <summary>
/// The scalar-wire adapter over <see cref="EntityCrudTestsBase{TEntity, T, TNullable}"/>:
/// numeric and reference strong types (<c>NonEmptyString</c> / <c>Email</c> /
/// <c>MailAddress</c>) that serialize as a single JSON scalar. It plugs a
/// <c>Create(TWire)</c> factory and two seeds (<see cref="FirstValid"/>,
/// <see cref="UpdatedValid"/>) into the shared create / get / update / PATCH suite, and adds
/// the scalar-only cases: the full <see cref="IEntityTestData{TWire}.ValidInputs"/> round-trip
/// and the malformed-scalar invalid payloads.
/// </summary>
/// <remarks>
/// <typeparamref name="TSelf"/> uses the curiously recurring template pattern so each concrete
/// subclass passes itself and must implement <see cref="IEntityTestData{TWire}"/> — a missing
/// data member is a CS0535 build error instead of a silently skipped test.
/// </remarks>
public abstract partial class EntityTests<TSelf, TEntity, T, TNullable, TWire>(TestWebApplicationFactory factory)
    : EntityCrudTestsBase<TEntity, T, TNullable>(factory)
    where TSelf : EntityTests<TSelf, TEntity, T, TNullable, TWire>, IEntityTestData<TWire>
    where TEntity : class, IEntity<TEntity, T, TNullable>
    where T : notnull
{
    /// <summary>Wraps a raw wire-format value in the strong type.</summary>
    protected abstract T Create(TWire raw);

    /// <summary>Baseline valid value seeding the shared create / get / update / PATCH suite.</summary>
    protected abstract TWire FirstValid { get; }

    /// <summary>Update/PATCH target; must differ from <see cref="FirstValid"/>.</summary>
    protected abstract TWire UpdatedValid { get; }

    protected sealed override object ValidBody => FirstValid!;
    protected sealed override T ValidValue => Create(FirstValid);
    protected sealed override object UpdatedBody => UpdatedValid!;
    protected sealed override T UpdatedValue => Create(UpdatedValid);

    protected sealed override void AssertJsonIsValidValue(JsonElement element) =>
        Assert.Equal(FirstValid, element.Deserialize<TWire>());

    // xUnit resolves these names on the concrete test class at runtime; the analyzer can't see
    // through the static abstract + CRTP, so we suppress xUnit1015 where we reference them.
    protected const string ValidInputsMember = nameof(IEntityTestData<TWire>.ValidInputs);
    protected const string InvalidInputsMember = nameof(IEntityTestData<TWire>.InvalidInputs);

#pragma warning disable xUnit1015

    // ── Create: valid across the whole ValidInputs set ───────────────────

    [Theory]
    [MemberData(ValidInputsMember)]
    public async Task ValidInput_PersistsInBothDatabases(TWire value)
    {
        var created = await Post(CreateEndpoint, new { value, nullableValue = value });
        var expected = Create(value);
        await AssertEntity(created.Id, expected, ToNullable(expected));
    }

    // ── Create: invalid ──────────────────────────────────────────────────
    // Each invalid value is tested in both slots independently; the other slot holds FirstValid
    // so the test isolates the one field being checked. A malformed non-null value fails inside
    // the JSON converter while positioned on the property, so System.Text.Json keys the error by
    // its path ("$.value" / "$.nullableValue") — the raw framework key, since this harness does
    // not call AddStrongTypes to normalize it.

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task InvalidValue_ReturnsBadRequest(TWire invalid)
    {
        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = (object?)invalid, nullableValue = (object?)FirstValid }, Ct);
        var errors = await AssertValidationProblem(response);
        Assert.Contains("$.value", errors.EnumerateObject().Select(p => p.Name));
    }

    [Theory]
    [MemberData(InvalidInputsMember)]
    public async Task InvalidNullableValue_ReturnsBadRequest(TWire invalid)
    {
        var response = await Client.PostAsJsonAsync(
            CreateEndpoint, new { value = (object?)FirstValid, nullableValue = (object?)invalid }, Ct);
        var errors = await AssertValidationProblem(response);
        Assert.Contains("$.nullableValue", errors.EnumerateObject().Select(p => p.Name));
    }
#pragma warning restore xUnit1015
}

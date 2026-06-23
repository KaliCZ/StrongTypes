using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.AspNetCore.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.AspNetCore.IntegrationTests.Tests;

/// <summary>
/// Locks the <c>ValidationProblemDetails</c> error keys strong types produce for
/// invalid JSON request bodies, in both normalization modes.
///
/// Without normalization three failure modes surface different raw keys:
/// <list type="bullet">
///   <item><description>A malformed non-null value fails inside the converter for
///     both reference and struct types — the System.Text.Json path <c>$.value</c>.
///     (Struct type-mismatch/null land here too thanks to the converter rethrow.)</description></item>
///   <item><description>A JSON <c>null</c> for a non-nullable reference type
///     deserializes, then fails the post-binding implicit-required check — the
///     C# property name <c>Value</c>, already free of a <c>$.</c> path.</description></item>
/// </list>
/// With normalization on (the default, PascalCase) every <c>$.</c> path collapses
/// to the C# property name, so all three modes converge on <c>Value</c> /
/// <c>NullableValue</c> — matching the keys data-annotation errors use.
/// </summary>
public sealed class JsonBodyErrorKeyTests
    : IClassFixture<NormalizedJsonErrorKeysFactory>, IClassFixture<RawJsonErrorKeysFactory>
{
    private readonly NormalizedJsonErrorKeysFactory _normalized;
    private readonly RawJsonErrorKeysFactory _raw;

    public JsonBodyErrorKeyTests(NormalizedJsonErrorKeysFactory normalized, RawJsonErrorKeysFactory raw)
    {
        _normalized = normalized;
        _raw = raw;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string NonEmptyString = "/json-body-probe/non-empty-string";
    private const string PositiveInt = "/json-body-probe/positive-int";

    // (endpoint, body, key without normalization, key with normalization [PascalCase default])
    private static readonly (string Endpoint, string Json, string RawKey, string NormalizedKey)[] Cases =
    [
        // Reference type: malformed non-null fails in the converter -> $.value.
        (NonEmptyString, """{"value":"","nullableValue":"ok"}""", "$.value", "Value"),
        (NonEmptyString, """{"value":"ok","nullableValue":"   "}""", "$.nullableValue", "NullableValue"),
        // Reference type: null deserializes, then fails implicit-required -> already "Value".
        (NonEmptyString, """{"value":null,"nullableValue":"ok"}""", "Value", "Value"),
        // Struct type: invariant failure -> converter throws while positioned -> $.value.
        (PositiveInt, """{"value":0,"nullableValue":5}""", "$.value", "Value"),
        // Struct type: type mismatch and null now also report $.value (converter rethrow).
        (PositiveInt, """{"value":"abc","nullableValue":5}""", "$.value", "Value"),
        (PositiveInt, """{"value":null,"nullableValue":5}""", "$.value", "Value"),
        (PositiveInt, """{"value":5,"nullableValue":0}""", "$.nullableValue", "NullableValue"),
    ];

    public static TheoryData<bool, string, string, string> ErrorKeyCases()
    {
        var data = new TheoryData<bool, string, string, string>();
        foreach (var (endpoint, json, rawKey, normalizedKey) in Cases)
        {
            data.Add(true, endpoint, json, normalizedKey);
            data.Add(false, endpoint, json, rawKey);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ErrorKeyCases))]
    public async Task InvalidJsonBody_ReportsExpectedErrorKey(
        bool normalize, string endpoint, string json, string expectedKey)
    {
        var client = (normalize ? (WebApplicationFactory<Program>)_normalized : _raw).CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(endpoint, content, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("One or more validation errors occurred.", problem.GetProperty("title").GetString());
        Assert.Equal(400, problem.GetProperty("status").GetInt32());

        var errors = problem.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        var keys = errors.EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Contains(expectedKey, keys);
    }

    // The target the default (PascalCase) normalization is matching: data-annotation
    // errors are keyed by the C# property name (Value, Email), with no $. path.
    // Strong-type body errors under PascalCase normalization land on the same keys,
    // so the two surfaces agree.
    [Fact]
    public async Task DataAnnotationErrors_AreKeyedByPascalCasePropertyName()
    {
        var client = _normalized.CreateClient();
        using var content = new StringContent("""{"email":"not-an-email"}""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/json-body-probe/data-annotations", content, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        var keys = problem.GetProperty("errors").EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Contains("Value", keys);  // [Required], value missing
        Assert.Contains("Email", keys);  // [EmailAddress], malformed
    }
}

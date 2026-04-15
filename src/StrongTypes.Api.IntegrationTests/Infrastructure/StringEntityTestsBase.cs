using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Models;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Base for StringEntity test classes. Centralizes the routes, the HTTP helper
/// names (Client and CancellationToken are captured implicitly), and the
/// AssertStringEntity check. Tests inheriting this can call Post/Put/Get
/// without specifying the response type — StringEntityResponse is baked in.
/// </summary>
public abstract class StringEntityTestsBase(TestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    protected const string NonNullable = "/string-entities/non-nullable";
    protected const string Nullable = "/string-entities/nullable";
    protected static string UpdateNonNullable(Guid id) => $"/string-entities/{id}/non-nullable";
    protected static string UpdateNullable(Guid id) => $"/string-entities/{id}/nullable";
    protected static string SqlServerGet(Guid id) => $"/string-entities/{id}/sql-server";
    protected static string PostgreSqlGet(Guid id) => $"/string-entities/{id}/postgresql";

    protected Task<StringEntityResponse> Post(string url, object body) =>
        Client.PostJsonAsync<StringEntityResponse>(url, body, Ct);

    protected Task<JsonElement> PostExpecting(string url, object body, HttpStatusCode expectedStatus) =>
        Client.PostJsonAsync<JsonElement>(url, body, Ct, expectedStatus);

    protected Task Put(string url, object body) =>
        Client.PutJsonAsync(url, body, Ct);

    protected Task<T> Get<T>(string url) =>
        Client.GetJsonAsync<T>(url, Ct);

    /// <summary>
    /// Fetches the entity with the given id from the supplied DbContext and asserts
    /// that its Value and NullableValue match the expected values.
    /// </summary>
    protected static async Task AssertStringEntity(
        DbContext db,
        Guid id,
        string expectedValue,
        string? expectedNullableValue)
    {
        var entity = await db.Set<StringEntity>().FindAsync([id], Ct);
        Assert.NotNull(entity);
        Assert.Equal(expectedValue, entity!.Value);
        Assert.Equal(expectedNullableValue, entity.NullableValue);
    }
}

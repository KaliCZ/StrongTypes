using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only HttpClient extensions that combine a request, a status-code assertion,
/// and (for read helpers) a typed body deserialization in a single call.
/// </summary>
public static class HttpClientTestExtensions
{
    public static async Task<T> PostJsonAsync<T>(
        this HttpClient client,
        string url,
        object body,
        CancellationToken ct,
        HttpStatusCode expectedStatus = HttpStatusCode.Created)
    {
        var response = await client.PostAsJsonAsync(url, body, ct);
        Assert.Equal(expectedStatus, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }

    public static async Task PutJsonAsync(
        this HttpClient client,
        string url,
        object body,
        CancellationToken ct,
        HttpStatusCode expectedStatus = HttpStatusCode.OK)
    {
        var response = await client.PutAsJsonAsync(url, body, ct);
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    public static async Task<T> GetJsonAsync<T>(
        this HttpClient client,
        string url,
        CancellationToken ct,
        HttpStatusCode expectedStatus = HttpStatusCode.OK)
    {
        var response = await client.GetAsync(url, ct);
        Assert.Equal(expectedStatus, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>(ct))!;
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace StrongTypes.AspNetCore.IntegrationTests.Tests.BindingTests;

internal static class BindingTestAsserts
{
    internal static async Task AssertValidationProblem(
        HttpResponseMessage response,
        string expectedField,
        string? expectedMessage = null)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var errors = problem.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        foreach (var prop in errors.EnumerateObject())
        {
            if (string.Equals(prop.Name, expectedField, StringComparison.OrdinalIgnoreCase))
            {
                if (expectedMessage is not null)
                {
                    var messages = prop.Value.EnumerateArray().Select(e => e.GetString()).ToArray();
                    Assert.Contains(expectedMessage, messages);
                }

                return;
            }
        }

        Assert.Fail($"Expected ValidationProblemDetails to include error for '{expectedField}'. Actual: {problem}");
    }
}

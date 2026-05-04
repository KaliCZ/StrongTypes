using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace StrongTypes.AspNetCore.IntegrationTests.Tests.BindingTests;

internal static class BindingTestAsserts
{
    internal static async Task AssertValidationProblem(HttpResponseMessage response, string expectedField)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var errors = problem.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        var found = false;
        foreach (var prop in errors.EnumerateObject())
        {
            if (string.Equals(prop.Name, expectedField, StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                break;
            }
        }
        Assert.True(found, $"Expected ValidationProblemDetails to include error for '{expectedField}'. Actual: {problem}");
    }
}

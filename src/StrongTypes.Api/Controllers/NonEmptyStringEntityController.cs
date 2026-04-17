using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-empty-string-entities")]
public sealed class NonEmptyStringEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonEmptyStringEntity, NonEmptyString, NonEmptyString?>(sqlCtx, pgCtx)
{
    // Filter routes share a {provider:regex(^sql-server$|^postgresql$)} segment
    // so each endpoint is declared once and dispatches to the right DbContext
    // at request time. The regex constraint keeps the route from colliding with
    // the existing /{id:guid}/{provider} routes and produces a 404 for an
    // unknown provider rather than a 500 from Set().
    private const string ProviderRoute = "{provider:regex(^sql-server$|^postgresql$)}";

    private DbSet<NonEmptyStringEntity> Set(string provider) => provider switch
    {
        "sql-server" => SqlSet,
        "postgresql" => PgSet,
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown provider."),
    };

    [HttpGet($"{ProviderRoute}/equal-to")]
    public Task<IActionResult> EqualTo(string provider, [FromQuery] string value)
    {
        var needle = NonEmptyString.Create(value);
        return Ids(Set(provider).Where(e => e.Value == needle));
    }

    [HttpGet($"{ProviderRoute}/not-equal-to")]
    public Task<IActionResult> NotEqualTo(string provider, [FromQuery] string value)
    {
        var needle = NonEmptyString.Create(value);
        return Ids(Set(provider).Where(e => e.Value != needle));
    }

    [HttpGet($"{ProviderRoute}/null-nullable")]
    public Task<IActionResult> NullNullable(string provider) =>
        Ids(Set(provider).Where(e => e.NullableValue == null));

    [HttpGet($"{ProviderRoute}/not-null-nullable")]
    public Task<IActionResult> NotNullNullable(string provider) =>
        Ids(Set(provider).Where(e => e.NullableValue != null));

    [HttpGet($"{ProviderRoute}/ordered")]
    public async Task<IActionResult> Ordered(string provider) =>
        Ok(await Set(provider).OrderBy(e => e.Value).Select(e => e.Id).ToListAsync());

    [HttpGet($"{ProviderRoute}/contains")]
    public Task<IActionResult> Contains(string provider, [FromQuery] string term) =>
        Ids(Set(provider).Where(e => e.Value.Unwrap().Contains(term)));

    [HttpGet($"{ProviderRoute}/starts-with")]
    public Task<IActionResult> StartsWith(string provider, [FromQuery] string prefix) =>
        Ids(Set(provider).Where(e => e.Value.Unwrap().StartsWith(prefix)));

    [HttpGet($"{ProviderRoute}/ends-with")]
    public Task<IActionResult> EndsWith(string provider, [FromQuery] string suffix) =>
        Ids(Set(provider).Where(e => e.Value.Unwrap().EndsWith(suffix)));

    [HttpGet($"{ProviderRoute}/like")]
    public Task<IActionResult> Like(string provider, [FromQuery] string pattern) =>
        Ids(Set(provider).Where(e => EF.Functions.Like(e.Value.Unwrap(), pattern)));

    private static async Task<IActionResult> Ids(IQueryable<NonEmptyStringEntity> query)
    {
        var ids = await query.Select(e => e.Id).ToListAsync();
        return new OkObjectResult(ids);
    }
}

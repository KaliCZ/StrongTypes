using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("string-entities")]
public class StringEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx) : ControllerBase
{
    [HttpGet("{id:guid}/sql-server")]
    public async Task<IActionResult> GetFromSqlServer(Guid id)
    {
        var entity = await sqlCtx.StringEntities.FindAsync(id);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpGet("{id:guid}/postgresql")]
    public async Task<IActionResult> GetFromPostgreSql(Guid id)
    {
        var entity = await pgCtx.StringEntities.FindAsync(id);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost("non-nullable")]
    public async Task<IActionResult> CreateNonNullable(CreateNonNullableRequest request)
    {
        var entity = new StringEntity(request.Value, request.NullableValue);
        sqlCtx.StringEntities.Add(entity);
        pgCtx.StringEntities.Add(entity);
        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();
        return Created($"/string-entities/{entity.Id}", new StringEntityResponse(entity.Id));
    }

    [HttpPost("nullable")]
    public async Task<IActionResult> CreateNullable(CreateNullableRequest request)
    {
        var entity = new StringEntity(request.Value, request.NullableValue);
        sqlCtx.StringEntities.Add(entity);
        pgCtx.StringEntities.Add(entity);
        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();
        return Created($"/string-entities/{entity.Id}", new StringEntityResponse(entity.Id));
    }

    [HttpPut("{id:guid}/non-nullable")]
    public async Task<IActionResult> UpdateNonNullable(Guid id, UpdateNonNullableRequest request)
    {
        var sqlEntity = await sqlCtx.StringEntities.FindAsync(id);
        var pgEntity = await pgCtx.StringEntities.FindAsync(id);
        if (sqlEntity is null || pgEntity is null)
            return NotFound();
        sqlEntity.Value = request.Value;
        sqlEntity.NullableValue = request.NullableValue;
        pgEntity.Value = request.Value;
        pgEntity.NullableValue = request.NullableValue;
        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();
        return Ok(new StringEntityResponse(id));
    }

    [HttpPut("{id:guid}/nullable")]
    public async Task<IActionResult> UpdateNullable(Guid id, UpdateNullableRequest request)
    {
        var sqlEntity = await sqlCtx.StringEntities.FindAsync(id);
        var pgEntity = await pgCtx.StringEntities.FindAsync(id);
        if (sqlEntity is null || pgEntity is null)
            return NotFound();
        sqlEntity.Value = request.Value;
        sqlEntity.NullableValue = request.NullableValue;
        pgEntity.Value = request.Value;
        pgEntity.NullableValue = request.NullableValue;
        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();
        return Ok(new StringEntityResponse(id));
    }
}

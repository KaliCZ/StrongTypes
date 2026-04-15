using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("string-entities")]
public class StringEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx) : ControllerBase
{
    [HttpPost("non-nullable")]
    public async Task<IActionResult> CreateNonNullable(CreateNonNullableRequest request)
    {
        var id = Guid.NewGuid();
        sqlCtx.StringEntities.Add(new StringEntity { Id = id, Value = request.Value, NullableValue = request.NullableValue });
        pgCtx.StringEntities.Add(new StringEntity { Id = id, Value = request.Value, NullableValue = request.NullableValue });
        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();
        return Created($"/string-entities/{id}", new StringEntityResponse(id));
    }

    [HttpPost("nullable")]
    public async Task<IActionResult> CreateNullable(CreateNullableRequest request)
    {
        var id = Guid.NewGuid();
        sqlCtx.StringEntities.Add(new StringEntity { Id = id, Value = request.Value, NullableValue = request.NullableValue });
        pgCtx.StringEntities.Add(new StringEntity { Id = id, Value = request.Value, NullableValue = request.NullableValue });
        await sqlCtx.SaveChangesAsync();
        await pgCtx.SaveChangesAsync();
        return Created($"/string-entities/{id}", new StringEntityResponse(id));
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

using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;
using StrongTypes.Api.Models;

namespace StrongTypes.Api.Controllers;

/// <summary>
/// Bespoke controller for <see cref="EmailEntity"/>: requests and responses are
/// shaped in <see cref="Email"/> (so the wrapper's JSON converter enforces the
/// 254-character cap and addr-spec parse on the way in) but the entity itself
/// stores the BCL <see cref="MailAddress"/> — the validation contract belongs
/// at the wire boundary, not on every read.
/// </summary>
[ApiController]
[Route("email-entities")]
public sealed class EmailEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<EmailEntity>(sqlCtx, pgCtx)
{
    [HttpGet("{id:guid}/sql-server")]
    public async Task<IActionResult> GetFromSqlServer(Guid id)
    {
        var entity = await SqlSet.FindAsync(id);
        return entity is null ? NotFound() : Ok(ToDto(entity));
    }

    [HttpGet("{id:guid}/postgresql")]
    public async Task<IActionResult> GetFromPostgreSql(Guid id)
    {
        var entity = await PgSet.FindAsync(id);
        return entity is null ? NotFound() : Ok(ToDto(entity));
    }

    [HttpPost]
    public async Task<IActionResult> Create(ReferenceEntityRequest<Email> request)
    {
        var entity = EmailEntity.Create(request.Value, request.NullableValue?.Value);
        SqlSet.Add(entity);
        PgSet.Add(entity);
        await SaveAllAsync();
        return Created($"{Request.Path}/{entity.Id}", new EntityResponse(entity.Id));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ReferenceEntityRequest<Email> request)
    {
        var sqlEntity = await SqlSet.FindAsync(id);
        var pgEntity = await PgSet.FindAsync(id);
        if (sqlEntity is null || pgEntity is null) return NotFound();
        sqlEntity.Update(request.Value, request.NullableValue?.Value);
        pgEntity.Update(request.Value, request.NullableValue?.Value);
        await SaveAllAsync();
        return Ok(new EntityResponse(id));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Patch(Guid id, ReferenceEntityPatchRequest<Email> request)
    {
        var sqlEntity = await SqlSet.FindAsync(id);
        var pgEntity = await PgSet.FindAsync(id);
        if (sqlEntity is null || pgEntity is null) return NotFound();

        if (request.Value is { } value)
        {
            sqlEntity.Value = value;
            pgEntity.Value = value;
        }

        if (request.NullableValue is { } nv)
        {
            sqlEntity.NullableValue = nv.Value?.Value;
            pgEntity.NullableValue = nv.Value?.Value;
        }

        await SaveAllAsync();
        return Ok(new EntityResponse(id));
    }

    /// <summary>
    /// DTO for the Email response. Custom shape (rather than a generic
    /// <see cref="StructEntityDto{T}"/>) because the entity stores a
    /// <see cref="MailAddress"/> reference but the wire surface is the
    /// <see cref="Email"/> wrapper.
    /// </summary>
    public sealed record EmailEntityDto(Guid Id, Email Value, Email? NullableValue);

    private static EmailEntityDto ToDto(EmailEntity entity) =>
        new(entity.Id, entity.Value, entity.NullableValue is { } nv ? (Email)nv : null);
}

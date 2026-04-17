using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("positive-short-entities")]
public sealed class PositiveShortEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<PositiveShortEntity, Positive<short>, Positive<short>?>(sqlCtx, pgCtx);

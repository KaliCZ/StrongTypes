using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("positive-long-entities")]
public sealed class PositiveLongEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<PositiveLongEntity, Positive<long>, Positive<long>?>(sqlCtx, pgCtx);

using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("positive-decimal-entities")]
public sealed class PositiveDecimalEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<PositiveDecimalEntity, Positive<decimal>, Positive<decimal>?>(sqlCtx, pgCtx);

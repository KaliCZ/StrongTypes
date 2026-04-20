using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-positive-decimal-entities")]
public sealed class NonPositiveDecimalEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : StructTypeEntityControllerBase<NonPositiveDecimalEntity, NonPositive<decimal>>(sqlCtx, pgCtx);

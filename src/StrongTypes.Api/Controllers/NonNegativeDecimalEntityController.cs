using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-negative-decimal-entities")]
public sealed class NonNegativeDecimalEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : StructTypeEntityControllerBase<NonNegativeDecimalEntity, NonNegative<decimal>>(sqlCtx, pgCtx);

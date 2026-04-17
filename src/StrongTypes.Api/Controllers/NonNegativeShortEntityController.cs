using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-negative-short-entities")]
public sealed class NonNegativeShortEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonNegativeShortEntity, NonNegative<short>, NonNegative<short>?>(sqlCtx, pgCtx);

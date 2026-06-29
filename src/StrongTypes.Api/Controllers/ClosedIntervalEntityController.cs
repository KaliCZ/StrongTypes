using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("closed-interval-entities")]
public sealed class ClosedIntervalEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : StructTypeEntityControllerBase<ClosedIntervalEntity, ClosedInterval<int>>(sqlCtx, pgCtx);

using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("interval-from-entities")]
public sealed class IntervalFromEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : StructTypeEntityControllerBase<IntervalFromEntity, IntervalFrom<int>>(sqlCtx, pgCtx);

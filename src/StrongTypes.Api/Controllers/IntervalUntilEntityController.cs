using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("interval-until-entities")]
public sealed class IntervalUntilEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : StructTypeEntityControllerBase<IntervalUntilEntity, IntervalUntil<int>>(sqlCtx, pgCtx);

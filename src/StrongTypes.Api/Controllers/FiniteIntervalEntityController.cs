using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("finite-interval-entities")]
public sealed class FiniteIntervalEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : StructTypeEntityControllerBase<FiniteIntervalEntity, FiniteInterval<int>>(sqlCtx, pgCtx);

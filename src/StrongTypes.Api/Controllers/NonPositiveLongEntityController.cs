using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-positive-long-entities")]
public sealed class NonPositiveLongEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonPositiveLongEntity, NonPositive<long>, NonPositive<long>?>(sqlCtx, pgCtx);

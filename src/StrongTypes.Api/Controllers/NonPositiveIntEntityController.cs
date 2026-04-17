using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-positive-int-entities")]
public sealed class NonPositiveIntEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonPositiveIntEntity, NonPositive<int>, NonPositive<int>?>(sqlCtx, pgCtx);

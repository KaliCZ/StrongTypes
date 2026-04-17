using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-positive-short-entities")]
public sealed class NonPositiveShortEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonPositiveShortEntity, NonPositive<short>, NonPositive<short>?>(sqlCtx, pgCtx);

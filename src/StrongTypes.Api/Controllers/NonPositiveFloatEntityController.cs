using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-positive-float-entities")]
public sealed class NonPositiveFloatEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonPositiveFloatEntity, NonPositive<float>, NonPositive<float>?>(sqlCtx, pgCtx);

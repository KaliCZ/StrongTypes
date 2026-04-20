using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-positive-float-entities")]
public sealed class NonPositiveFloatEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : StructTypeEntityControllerBase<NonPositiveFloatEntity, NonPositive<float>>(sqlCtx, pgCtx);

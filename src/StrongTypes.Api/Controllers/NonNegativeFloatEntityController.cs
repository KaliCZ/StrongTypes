using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-negative-float-entities")]
public sealed class NonNegativeFloatEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonNegativeFloatEntity, NonNegative<float>, NonNegative<float>?>(sqlCtx, pgCtx);

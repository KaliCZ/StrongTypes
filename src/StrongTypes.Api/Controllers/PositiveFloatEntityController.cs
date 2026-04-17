using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("positive-float-entities")]
public sealed class PositiveFloatEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<PositiveFloatEntity, Positive<float>, Positive<float>?>(sqlCtx, pgCtx);

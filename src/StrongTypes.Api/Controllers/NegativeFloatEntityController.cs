using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("negative-float-entities")]
public sealed class NegativeFloatEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NegativeFloatEntity, Negative<float>, Negative<float>?>(sqlCtx, pgCtx);

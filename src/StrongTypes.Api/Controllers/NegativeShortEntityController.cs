using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("negative-short-entities")]
public sealed class NegativeShortEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NegativeShortEntity, Negative<short>, Negative<short>?>(sqlCtx, pgCtx);

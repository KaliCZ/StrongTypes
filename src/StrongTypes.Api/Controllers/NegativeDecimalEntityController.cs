using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("negative-decimal-entities")]
public sealed class NegativeDecimalEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NegativeDecimalEntity, Negative<decimal>, Negative<decimal>?>(sqlCtx, pgCtx);

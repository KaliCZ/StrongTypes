using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("positive-double-entities")]
public sealed class PositiveDoubleEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<PositiveDoubleEntity, Positive<double>, Positive<double>?>(sqlCtx, pgCtx);

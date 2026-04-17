using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-positive-double-entities")]
public sealed class NonPositiveDoubleEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonPositiveDoubleEntity, NonPositive<double>, NonPositive<double>?>(sqlCtx, pgCtx);

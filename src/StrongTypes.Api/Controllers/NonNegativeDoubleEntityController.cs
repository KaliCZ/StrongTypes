using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-negative-double-entities")]
public sealed class NonNegativeDoubleEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonNegativeDoubleEntity, NonNegative<double>, NonNegative<double>?>(sqlCtx, pgCtx);

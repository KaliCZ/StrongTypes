using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-negative-int-entities")]
public sealed class NonNegativeIntEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : ValueEntityControllerBase<NonNegativeIntEntity, NonNegative<int>>(sqlCtx, pgCtx);

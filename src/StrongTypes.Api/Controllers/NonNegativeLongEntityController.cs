using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-negative-long-entities")]
public sealed class NonNegativeLongEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonNegativeLongEntity, NonNegative<long>, NonNegative<long>?>(sqlCtx, pgCtx);

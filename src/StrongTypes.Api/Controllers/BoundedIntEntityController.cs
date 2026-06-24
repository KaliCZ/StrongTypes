using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("bounded-int-entities")]
public sealed class BoundedIntEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : StructTypeEntityControllerBase<BoundedIntEntity, BoundedInt<PageSizeBounds>>(sqlCtx, pgCtx);

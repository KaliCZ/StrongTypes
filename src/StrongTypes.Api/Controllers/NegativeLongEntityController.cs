using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("negative-long-entities")]
public sealed class NegativeLongEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NegativeLongEntity, Negative<long>, Negative<long>?>(sqlCtx, pgCtx);

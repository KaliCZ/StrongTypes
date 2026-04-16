using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("negative-int-entities")]
public sealed class NegativeIntEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : ValueEntityControllerBase<NegativeIntEntity, Negative<int>>(sqlCtx, pgCtx);

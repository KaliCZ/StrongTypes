using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("negative-double-entities")]
public sealed class NegativeDoubleEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NegativeDoubleEntity, Negative<double>, Negative<double>?>(sqlCtx, pgCtx);

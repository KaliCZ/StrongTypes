using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("email-entities")]
public sealed class EmailEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : StructTypeEntityControllerBase<EmailEntity, Email>(sqlCtx, pgCtx);

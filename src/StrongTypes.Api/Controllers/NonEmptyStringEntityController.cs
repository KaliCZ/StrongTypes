using Microsoft.AspNetCore.Mvc;
using StrongTypes.Api.Data;
using StrongTypes.Api.Entities;

namespace StrongTypes.Api.Controllers;

[ApiController]
[Route("non-empty-string-entities")]
public sealed class NonEmptyStringEntityController(SqlServerDbContext sqlCtx, PostgreSqlDbContext pgCtx)
    : EntityControllerBase<NonEmptyStringEntity, NonEmptyString, NonEmptyString?>(sqlCtx, pgCtx);

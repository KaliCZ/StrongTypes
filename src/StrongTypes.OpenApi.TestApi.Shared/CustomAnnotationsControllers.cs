using Microsoft.AspNetCore.Mvc;

namespace StrongTypes.OpenApi.TestApi.Shared;


[ApiController]
[Route("annotated-texts")]
public sealed class AnnotatedTextsController : ControllerBase
{
    [HttpPost]
    public IActionResult Echo(AnnotatedTextsRequest request) => Ok(request);
}

[ApiController]
[Route("annotated-numbers")]
public sealed class AnnotatedNumbersController : ControllerBase
{
    [HttpPost]
    public IActionResult Echo(AnnotatedNumbersRequest request) => Ok(request);
}

[ApiController]
[Route("annotated-tags")]
public sealed class AnnotatedTagsController : ControllerBase
{
    [HttpPost]
    public IActionResult Echo(AnnotatedTagsRequest request) => Ok(request);
}

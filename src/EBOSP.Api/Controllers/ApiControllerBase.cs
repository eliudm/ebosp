using Microsoft.AspNetCore.Mvc;

namespace EBOSP.Api.Controllers;

/// <summary>
/// Base type every controller must inherit (ADR-0003: URL path versioning). [ApiController] also
/// gives automatic model-state validation - an invalid request short-circuits to a 400
/// ValidationProblemDetails response before the action runs, which is the foundation module's
/// validation pipeline (dev guide §9) until a use case needs richer, cross-field business rule
/// validation (added with FluentValidation when the first such use case lands).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class ApiControllerBase : ControllerBase;

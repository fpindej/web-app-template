using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyProject.WebApi.Shared;

// This class can be used to define common functionality for all V1 controllers.
// For example, you can add common methods, properties, or filters that should apply to all V1 controllers.

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public abstract class ApiController : ControllerBase;

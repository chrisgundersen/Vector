using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Vector.Api.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Returns the health status of the API.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        return Ok(new HealthResponse(
            Status: "Healthy",
            Version: "1.0.0",
            Timestamp: DateTime.UtcNow));
    }
}

public record HealthResponse(string Status, string Version, DateTime Timestamp);

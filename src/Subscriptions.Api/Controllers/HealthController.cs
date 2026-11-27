using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Subscriptions.Business.Abstractions;
using Subscriptions.Api.Contracts;
namespace Subscriptions.Api.Controllers;


[ApiController]
[Route("api/v1/health")]
public sealed class HealthController(IReadinessProbe probe) : ControllerBase
{
    [HttpGet("ready"), AllowAnonymous]
    [ProducesResponseType(typeof(ReadyResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 503)]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        if (!await probe.IsReadyAsync(ct))
            return StatusCode(503, new ErrorResponse("database_unavailable", "База не готова.", HttpContext.TraceIdentifier));
        return Ok(new ReadyResponse("ready"));
    }
}

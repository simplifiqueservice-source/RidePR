using Microsoft.AspNetCore.Mvc;

namespace RidePR.Api.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            application = "RidePR Enterprise",
            version = "1.0.0",
            status = "Online",
            serverTime = DateTime.UtcNow
        });
    }
}
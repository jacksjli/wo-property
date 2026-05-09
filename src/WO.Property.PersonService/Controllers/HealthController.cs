using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WO.Property.PersonService.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "PersonService",
            version = "1.0.0",
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
}
using Microsoft.AspNetCore.Mvc;

namespace Dragonscale_Storyteller.Controllers;

[ApiController]
[Route("[api]")]
public class DragonscaleController :ControllerBase

{
    [HttpGet("/helloworld")]
    public ActionResult<string> HelloWorld()
    {
        return "Hello from the other side.";
    }
}

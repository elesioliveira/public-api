using Microsoft.AspNetCore.Mvc;

namespace rmaesolutions.Controllers;


[ApiController]
public class StaticContentController : ControllerBase
{

    [HttpGet]
    [Route("v1/logo")]
    public IResult GetLogo()
    {
            return Results.File(Environment.CurrentDirectory + "/wwwroot/assets/images/rma_logo.png", "image/png");
    }

    [HttpGet]
    [Route("v1/favicon")]
    public IResult GetFavicon()
    {
            return Results.File(Environment.CurrentDirectory + "/wwwroot/assets/images/favicon.ico", "image/x-icon");
    }
  
}
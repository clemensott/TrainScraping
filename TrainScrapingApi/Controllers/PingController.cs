using Microsoft.AspNetCore.Mvc;

namespace TrainScrapingApi.Controllers
{
    [Route("[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            return "Success";
        }
    }
}

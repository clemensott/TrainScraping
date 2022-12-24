using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TrainScrapingApi.Helpers;
using TrainScrapingCommon.Models.RequestBody;

namespace TrainScrapingApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TrainsController : ControllerBase
    {
        [HttpPost("dny")]
        public async Task<IActionResult> PostDny([FromBody] PostDnyBody body)
        {
            await DnyHelper.Insert(body.Dny, body.Timestamp);
            return Ok();
        }
    }
}

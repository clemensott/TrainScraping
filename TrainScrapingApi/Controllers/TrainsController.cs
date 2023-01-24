using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TrainScrapingApi.Dnys;
using TrainScrapingApi.Helpers;
using TrainScrapingCommon.Models.Dnys;
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
            if (!await this.IsAuthenticatedAsync()) return Unauthorized();
            await DnyImportHelper.Insert(body.Dny, body.Timestamp);
            return Ok();
        }

        [HttpGet("dnyMetas")]
        [EnableCors("openrailwaymap")]
        public async Task<ActionResult<IEnumerable<DnyMeta>>> GetDnyMetas(
            [FromQuery][Required] DateTime rangeStart, 
            [FromQuery] DateTime? rangeEnd,
            [FromQuery] int limit = 500)
        {
            if (!await this.IsAuthenticatedAsync()) return Unauthorized();

            return (await DnyGetter.GetMetas(rangeStart, rangeEnd, limit)).ToArray();
        }

        [HttpGet("dnys")]
        [EnableCors("openrailwaymap")]
        public async Task<ActionResult<IEnumerable<Dny>>> GetDnys([FromQuery][Required] int[] ids)
        {
            if (!await this.IsAuthenticatedAsync()) return Unauthorized();

            return (await DnyGetter.GetDnys(ids)).ToArray();
        }
    }
}

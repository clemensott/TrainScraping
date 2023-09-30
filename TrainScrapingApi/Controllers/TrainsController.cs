using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TrainScrapingApi.Services.Database;
using TrainScrapingCommon.Models.Dnys;
using TrainScrapingCommon.Models.RequestBody;

namespace TrainScrapingApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TrainsController : ControllerBase
    {
        private readonly IDnyRepo dnyRepo;

        public TrainsController(IDnyRepo dnyRepo)
        {
            this.dnyRepo = dnyRepo;
        }

        [HttpPost("dny")]
        public async Task<IActionResult> PostDny([FromBody] PostDnyBody body)
        {
            await dnyRepo.Insert(body.Dny, body.Timestamp);
            return Ok();
        }

        [HttpGet("dnyMetas")]
        [EnableCors("openrailwaymap")]
        public async Task<ActionResult<IEnumerable<DnyMeta>>> GetDnyMetas(
            [FromQuery][Required] DateTime rangeStart, 
            [FromQuery] DateTime? rangeEnd,
            [FromQuery] int limit = 500)
        {
            return (await dnyRepo.GetMetas(rangeStart, rangeEnd, limit)).ToArray();
        }

        [HttpGet("dnys")]
        [EnableCors("openrailwaymap")]
        public async Task<ActionResult<IEnumerable<Dny>>> GetDnys([FromQuery][Required] int[] ids)
        {
            return (await dnyRepo.GetDnys(ids)).ToArray();
        }
    }
}

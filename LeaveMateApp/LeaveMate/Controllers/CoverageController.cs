using LeaveMate.Services.Integration;
using LeaveMate.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaveMate.Controllers
{
    /// <summary>
    /// Serves the Real-Time Coverage Matrix (Key Functionality #3) from the
    /// background-refreshed cache, so calendar loads are instant.
    /// </summary>
    [ApiController]
    [Route("api/coverage")]
    public class CoverageController : ControllerBase
    {
        private readonly CoverageCache _cache;

        public CoverageController(CoverageCache cache)
        {
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Get()
        {
            if (HttpContext.Session.GetActiveEmployeeId() is null) return Unauthorized();

            return Ok(new
            {
                lastRefreshedUtc = _cache.LastRefreshedUtc,
                days = _cache.GetSnapshot()
            });
        }
    }
}

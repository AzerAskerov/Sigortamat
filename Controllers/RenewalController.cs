using Microsoft.AspNetCore.Mvc;
using Sigortamat.Services;
using System.Threading.Tasks;

namespace Sigortamat.Controllers
{
    [ApiController]
    [Route("api/renewal")]
    public class RenewalController : ControllerBase
    {
        private readonly RenewalTrackingService _trackingService;

        public RenewalController(RenewalTrackingService trackingService)
        {
            _trackingService = trackingService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartTracking(string carNumber)
        {
            await _trackingService.StartRenewalTrackingAsync(carNumber);
            return Ok(new { message = $"Tracking started for {carNumber}" });
        }
    }
} 
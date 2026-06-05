using _211system.DTOs;
using _211system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransportController : ControllerBase
    {
        private readonly ITransportService _transportService;

        public TransportController(ITransportService transportService)
        {
            _transportService = transportService;
        }

        [HttpPost("record")]
        public async Task<IActionResult> RecordTransport([FromBody] RecordTransportDto dto)
        {
            try
            {
                await _transportService.RecordAsync(dto);
                return Ok(new { message = "Zapisano cel transportu." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

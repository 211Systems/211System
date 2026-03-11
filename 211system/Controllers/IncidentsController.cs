using _211system.DTOs.CPR112;
using _211system.Services;
using Microsoft.AspNetCore.Mvc;

namespace _211system.Controllers
{
    [ApiController]
    [Route("api/CPR112/[controller]")] 
    public class IncidentsController : ControllerBase
    {
        private readonly IIncidentService _incidentService;

        public IncidentsController(IIncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        [HttpPost]
        public async Task<ActionResult<IncidentDto>> CreateIncident([FromBody] CreateIncidentDto dto)
        {
            var result = await _incidentService.CreateIncidentAsync(dto);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IncidentDto>> GetIncidentById(Guid id)
        {
            try
            {
                var result = await _incidentService.GetIncidentByIdAsync(id);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeIncidentStatusDto dto)
        {
            try
            {
                await _incidentService.ChangeIncidentStatusAsync(id, dto);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
using System.Security.Claims;
using _211system.Data;
using _211system.DTOs.CPR112;
using _211system.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _211system.Models;

namespace _211system.Controllers
{
    [Authorize(Roles = "Dyspozytor112, Admin")]
    [ApiController]
    [Route("api/CPR112/[controller]")]
    public class IncidentsController : Controller
    {
        private readonly IIncidentService _incidentService;
        private readonly _211DbContext _context;
        public IncidentsController(IIncidentService incidentService, _211DbContext context)
        {
            _incidentService = incidentService;
            _context = context;
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

                var ApplicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (ApplicationUserId == null)
                    return Unauthorized("Brak autoryzacji (niewłaściwy lub brakujący token).");

                var currentOperator = await _context.Operators112
                    .FirstOrDefaultAsync(o => o.OpAccountId == ApplicationUserId);

                if (currentOperator == null)
                    return Forbid("Zalogowane konto nie jest przypisane do profilu dyspozytora 112.");

                await _incidentService.ChangeIncidentStatusAsync(id, currentOperator.Id, dto);

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
